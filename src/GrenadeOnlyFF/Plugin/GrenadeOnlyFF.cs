using System;
using System.Collections.Generic;

// TODO: Заменить на реальные неймспейсы из CounterStrikeSharp.dll
// Пример:
// using CounterStrikeSharp.API;
// using CounterStrikeSharp.API.Events;
// using CounterStrikeSharp.API.Plugins;
// using CounterStrikeSharp.API.Entities;

namespace GrenadeOnlyFF
{
    // TODO: Заменить BasePlugin на реальный базовый класс плагина из API
    // Например: public class GrenadeOnlyFF : PluginBase
    public class GrenadeOnlyFF /* : BasePlugin */ 
    {
        // Если API требует атрибуты или регистрацию — добавь их здесь.
        // Простейшая логика вынесена в методы, которые нужно привязать к событиям API.

        private bool _logAttempts = true;
        private HashSet<string> _allowedWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hegrenade",
            "hegrenade_thrown",
            "molotov",
            "incendiarygrenade",
            "incendiarygrenade_thrown"
        };

        // TODO: В API может быть метод OnLoad/OnEnable — переименуй/подпиши под API
        public void OnLoad()
        {
            // TODO: Подпишись на события через EventManager API
            // Пример (заменить на реальные вызовы):
            // EventManager.Subscribe("player_hurt", OnPlayerHurt);
            // EventManager.Subscribe("player_blind", OnPlayerBlind);
        }

        // TODO: В API может быть метод OnUnload/OnDisable — переименуй/подпиши под API
        public void OnUnload()
        {
            // TODO: Отписка от событий
            // EventManager.Unsubscribe("player_hurt", OnPlayerHurt);
            // EventManager.Unsubscribe("player_blind", OnPlayerBlind);
        }

        // Пример обработчика — адаптируй сигнатуру под реальный Event type
        // TODO: заменить тип EventType на реальный тип события из API
        private void OnPlayerHurt(object ev /* EventType ev */)
        {
            // TODO: заменить доступ к полям события на реальные имена
            // Пример псевдокода:
            // int victimUserId = ev.GetInt("userid");
            // int attackerUserId = ev.GetInt("attacker");
            // int damage = ev.GetInt("dmg_health");
            // string weapon = ev.GetString("weapon");

            // TODO: получить client id через Engine API
            // int victim = Engine.GetClientFromUserId(victimUserId);
            // int attacker = Engine.GetClientFromUserId(attackerUserId);

            // TODO: проверить валидность, команды и разрешённые оружия
            // Если тиммейт и weapon не в _allowedWeapons — обнулить урон:
            // ev.SetInt("dmg_health", 0);
            // ev.SetInt("dmg_armor", 0);

            // Логирование:
            // if (_logAttempts) Logger.Info($"Blocked team damage: {attackerName} -> {victimName} weapon={weapon} dmg={damage}");
        }

        private void OnPlayerBlind(object ev /* EventType ev */)
        {
            // TODO: аналогично — обнулить duration для тиммейтов
            // ev.SetFloat("duration", 0.0f);
            // if (_logAttempts) Logger.Info($"Blocked team flash: {attackerName} -> {victimName}");
        }

        // Вспомогательные методы (пример)
        private bool IsAllowedGrenade(string weapon)
        {
            if (string.IsNullOrEmpty(weapon)) return false;
            return _allowedWeapons.Contains(weapon);
        }
    }
}
