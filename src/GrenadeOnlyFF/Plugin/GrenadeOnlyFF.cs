using System;
using System.Collections.Generic;
using CounterStrikeSharp; // замените на реальный неймспейс вашей версии
using CounterStrikeSharp.Events;
using CounterStrikeSharp.Entities;
using CounterStrikeSharp.Plugins;

namespace GrenadeOnlyFF
{
    public class GrenadeOnlyFF : Plugin
    {
        private bool _logAttempts = true;
        private HashSet<string> _allowedWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hegrenade",
            "hegrenade_thrown",
            "molotov",
            "incendiarygrenade",
            "incendiarygrenade_thrown"
        };

        public override void OnLoad()
        {
            EventManager.Subscribe("player_hurt", OnPlayerHurt);
            EventManager.Subscribe("player_blind", OnPlayerBlind);
            Logger.Info("GrenadeOnlyFF loaded");
        }

        public override void OnUnload()
        {
            EventManager.Unsubscribe("player_hurt", OnPlayerHurt);
            EventManager.Unsubscribe("player_blind", OnPlayerBlind);
            Logger.Info("GrenadeOnlyFF unloaded");
        }

        private void OnPlayerHurt(Event ev)
        {
            int victimUserId = ev.GetInt("userid");
            int attackerUserId = ev.GetInt("attacker");
            int damage = ev.GetInt("dmg_health");
            string weapon = ev.GetString("weapon");

            int victim = Engine.GetClientFromUserId(victimUserId);
            int attacker = Engine.GetClientFromUserId(attackerUserId);

            if (!IsValidClient(victim) || !IsValidClient(attacker)) return;
            if (victim == attacker) return;
            if (Engine.GetTeam(attacker) != Engine.GetTeam(victim)) return;

            if (IsAllowedGrenade(weapon))
            {
                return;
            }

            ev.SetInt("dmg_health", 0);
            ev.SetInt("dmg_armor", 0);

            if (_logAttempts)
            {
                Logger.Info($"[GrenadeOnlyFF] Blocked team damage: {Engine.GetClientName(attacker)} -> {Engine.GetClientName(victim)} weapon={weapon} dmg={damage}");
            }
        }

        private void OnPlayerBlind(Event ev)
        {
            int victimUserId = ev.GetInt("userid");
            int attackerUserId = ev.GetInt("attacker");

            int victim = Engine.GetClientFromUserId(victimUserId);
            int attacker = Engine.GetClientFromUserId(attackerUserId);

            if (!IsValidClient(victim) || !IsValidClient(attacker)) return;
            if (victim == attacker) return;
            if (Engine.GetTeam(attacker) != Engine.GetTeam(victim)) return;

            ev.SetFloat("duration", 0.0f);

            if (_logAttempts)
            {
                Logger.Info($"[GrenadeOnlyFF] Blocked team flash: {Engine.GetClientName(attacker)} -> {Engine.GetClientName(victim)}");
            }
        }

        private bool IsValidClient(int client)
        {
            return client > 0 && Engine.IsClientInGame(client) && !Engine.IsClientBot(client);
        }

        private bool IsAllowedGrenade(string weapon)
        {
            if (string.IsNullOrEmpty(weapon)) return false;
            return _allowedWeapons.Contains(weapon);
        }
    }
}
