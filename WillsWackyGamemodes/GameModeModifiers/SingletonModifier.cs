using BepInEx.Bootstrap;
using Photon.Pun;
using RWF;
using RWF.GameModes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;

namespace WWGM.GameModeModifiers
{
    /// <summary>
    /// A simple gamemode modifier that restricts players from being given cards that already exist in the hand of any player.
    /// </summary>
    public static class SingletonModifier
    {
        internal const string ConfigSection = "Modifiers.Singleton";

        public static Config<bool> enabled;
        public static Config<bool> selfEnabled;
        public static Config<bool> classEnabled;

        public static void Setup()
        {
            enabled = ConfigManager.Bind<bool>(ConfigSection, "Enabled", false, "Whether the modifier is enabled or not.");
            selfEnabled = ConfigManager.Bind<bool>(ConfigSection, "OthersOnly", false, "Whether the singleton modifier affects cards that you have.");
            classEnabled = ConfigManager.Bind<bool>(ConfigSection, "IgnoreClasses", false, "Whether the singleton modifier affects class cards.");
        }

        public static bool Condition(Player player, CardInfo card)
        {
            if (!enabled.CurrentValue)
            {
                return true;
            }

            if (!card || !player || !player.data || !player.data.block || (player.data.currentCards == null))
            {
                return true;
            }

            // If we're allowed duplicates of our own card
            if ((selfEnabled.CurrentValue))
            {
                // If anyone else has it, we're not allowed it.
                if (PlayerManager.instance.players.Where(p => p != player).Any(p => p.data.currentCards.Contains(card)))
                {
                    return false;
                }
            }
            // If we're allowed duplicates of class cards
            else if (Chainloader.PluginInfos.Keys.Contains("root.classes.manager.reborn") && classEnabled.CurrentValue && ClassesManagerHelper.IsClassCard(card))
            {
                // If anyone else has it, we're not allowed it.
                if (PlayerManager.instance.players.Where(p => p != player).Any(p => p.data.currentCards.Contains(card)))
                {
                    return false;
                }
            }
            else if ((PlayerManager.instance.players.Any(p => p.data.currentCards.Contains(card))))
            {
                return false;
            }
            

            return true;
        }
    }
}
