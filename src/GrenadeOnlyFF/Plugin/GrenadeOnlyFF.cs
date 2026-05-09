using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// TODO: если реальные неймспейсы в CounterStrikeSharp отличаются — замените using ниже.
// Примеры: CounterStrikeSharp.API, CounterStrikeSharp.Plugins, CounterStrikeSharp.Events
using CounterStrikeSharp;            // предполагаемый основной неймспейс
using CounterStrikeSharp.Plugins;    // Plugin / PluginBase
using CounterStrikeSharp.Events;     // Event, EventManager
using CounterStrikeSharp.Entities;   // Engine, Player
using CounterStrikeSharp.Logging;    // Logger

namespace GrenadeOnlyFF
{
    public class PluginConfig
    {
        public bool Enabled { get; set; } = true;
        public bool LogAttempts { get; set; } = true;
        public List<string> AllowedWeapons { get; set; } = new List<string>
        {
            "hegrenade",
            "hegrenade_thrown",
            "molotov",
            "incendiarygrenade",
            "incendiarygrenade_thrown"
        };
        public bool ConfigReloadOnChange { get; set; } = true;
    }

    // Замените PluginBase на реальный базовый класс плагина в вашей версии CounterStrikeSharp
    public class GrenadeOnlyFF : PluginBase
    {
        private PluginConfig _config = new PluginConfig();
        private readonly string _configPath;
        private FileSystemWatcher? _watcher;
        private readonly object _cfgLock = new object();

        public GrenadeOnlyFF()
        {
            // По умолчанию ищем конфиг в стандартной папке CounterStrikeSharp configs/plugins/<PluginName>/
            // Если API предоставляет путь к конфигам — замените на API путь.
            var root = Path.Combine("configs", "plugins", "GrenadeOnlyFF");
            Directory.CreateDirectory(root);
            _configPath = Path.Combine(root, "pluginsettings.json");

            // Если в сборке есть встроенный конфиг (в bin) — скопируем его при отсутствии
            try
            {
                var local = Path.Combine(AppContext.BaseDirectory, "Config", "pluginsettings.json");
                if (File.Exists(local) && !File.Exists(_configPath))
                {
                    File.Copy(local, _configPath);
                }
            }
            catch { /* ignore */ }
        }

        public override void OnLoad()
        {
            LoadConfig();

            if (_config.ConfigReloadOnChange)
                StartWatcher();

            if (_config.Enabled)
            {
                SubscribeEvents();
                Logger.Info("GrenadeOnlyFF loaded and enabled.");
            }
            else
            {
                Logger.Info("GrenadeOnlyFF loaded but disabled by config.");
            }
        }

        public override void OnUnload()
        {
            UnsubscribeEvents();
            StopWatcher();
            Logger.Info("GrenadeOnlyFF unloaded.");
        }

        private void SubscribeEvents()
        {
            try
            {
                EventManager.Subscribe("player_hurt", OnPlayerHurt);
                EventManager.Subscribe("player_blind", OnPlayerBlind);
            }
            catch (Exception ex)
            {
                Logger.Error("GrenadeOnlyFF: failed to subscribe events: " + ex);
            }
        }

        private void UnsubscribeEvents()
        {
            try
            {
                EventManager.Unsubscribe("player_hurt", OnPlayerHurt);
                EventManager.Unsubscribe("player_blind", OnPlayerBlind);
            }
            catch { /* ignore */ }
        }

        private void LoadConfig()
        {
            lock (_cfgLock)
            {
                try
                {
                    if (!File.Exists(_configPath))
                    {
                        // создаём дефолтный конфиг
                        var defaultJson = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_configPath, defaultJson);
                        Logger.Info("GrenadeOnlyFF: default config created at " + _configPath);
                        return;
                    }

                    var json = File.ReadAllText(_configPath);
                    var cfg = JsonSerializer.Deserialize<PluginConfig>(json);
                    if (cfg != null)
                    {
                        _config = cfg;
                        Logger.Info("GrenadeOnlyFF: config loaded. enabled=" + _config.Enabled);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("GrenadeOnlyFF: failed to load config: " + ex);
                }
            }
        }

        private void StartWatcher()
        {
            try
            {
                var dir = Path.GetDirectoryName(_configPath) ?? ".";
                _watcher = new FileSystemWatcher(dir, Path.GetFileName(_configPath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes
                };
                _watcher.Changed += OnConfigChanged;
                _watcher.EnableRaisingEvents = true;
                Logger.Info("GrenadeOnlyFF: config watcher started.");
            }
            catch (Exception ex)
            {
                Logger.Error("GrenadeOnlyFF: failed to start config watcher: " + ex);
            }
        }

        private void StopWatcher()
        {
            try
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                    _watcher = null;
                }
            }
            catch { }
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce: подождём 200ms чтобы файл дописался
            Task.Delay(200).ContinueWith(_ =>
            {
                LoadConfig();
                // если включили/выключили — применяем
                if (_config.Enabled)
                {
                    SubscribeEvents();
                    Logger.Info("GrenadeOnlyFF: enabled via config reload.");
                }
                else
                {
                    UnsubscribeEvents();
                    Logger.Info("GrenadeOnlyFF: disabled via config reload.");
                }
            });
        }

        // Обработчик player_hurt
        private void OnPlayerHurt(Event ev)
        {
            try
            {
                if (!_config.Enabled) return;

                int victimUserId = ev.GetInt("userid");
                int attackerUserId = ev.GetInt("attacker");
                int damage = ev.GetInt("dmg_health");
                string weapon = ev.GetString("weapon") ?? string.Empty;

                int victim = Engine.GetClientFromUserId(victimUserId);
                int attacker = Engine.GetClientFromUserId(attackerUserId);

                if (!IsValidClient(victim) || !IsValidClient(attacker)) return;
                if (victim == attacker) return;
                if (Engine.GetTeam(attacker) != Engine.GetTeam(victim)) return;

                if (IsAllowedGrenade(weapon)) return;

                // Обнуляем урон
                ev.SetInt("dmg_health", 0);
                ev.SetInt("dmg_armor", 0);

                if (_config.LogAttempts)
                {
                    Logger.Info($"[GrenadeOnlyFF] Blocked team damage: {Engine.GetClientName(attacker)} -> {Engine.GetClientName(victim)} weapon={weapon} dmg={damage}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GrenadeOnlyFF: OnPlayerHurt exception: " + ex);
            }
        }

        // Обработчик player_blind (flash)
        private void OnPlayerBlind(Event ev)
        {
            try
            {
                if (!_config.Enabled) return;

                int victimUserId = ev.GetInt("userid");
                int attackerUserId = ev.GetInt("attacker");

                int victim = Engine.GetClientFromUserId(victimUserId);
                int attacker = Engine.GetClientFromUserId(attackerUserId);

                if (!IsValidClient(victim) || !IsValidClient(attacker)) return;
                if (victim == attacker) return;
                if (Engine.GetTeam(attacker) != Engine.GetTeam(victim)) return;

                // Обнуляем длительность флеша
                ev.SetFloat("duration", 0.0f);

                if (_config.LogAttempts)
                {
                    Logger.Info($"[GrenadeOnlyFF] Blocked team flash: {Engine.GetClientName(attacker)} -> {Engine.GetClientName(victim)}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GrenadeOnlyFF: OnPlayerBlind exception: " + ex);
            }
        }

        private bool IsValidClient(int client)
        {
            try
            {
                return client > 0 && Engine.IsClientInGame(client) && !Engine.IsClientBot(client);
            }
            catch
            {
                return client > 0;
            }
        }

        private bool IsAllowedGrenade(string weapon)
        {
            if (string.IsNullOrEmpty(weapon)) return false;
            lock (_cfgLock)
            {
                return _config.AllowedWeapons.Contains(weapon, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
