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
        public static bool enabled = false;
        public static bool SelfEnabled = false;
        public static bool classEnabled = false;

        public static bool Condition(Player player, CardInfo card)
        {
            if (!enabled)
            {
                return true;
            }

            if (!card || !player || !player.data || !player.data.block || (player.data.currentCards == null))
            {
                return true;
            }

            // If we're allowed duplicates of our own card
            if ((SelfEnabled))
            {
                // If anyone else has it, we're not allowed it.
                if (PlayerManager.instance.players.Where(p => p != player).Any(p => p.data.currentCards.Contains(card)))
                {
                    return false;
                }
            }
            // If we're allowed duplicates of class cards
            else if (Chainloader.PluginInfos.Keys.Contains("root.classes.manager.reborn") && classEnabled && ClassesManagerReborn.ClassesRegistry.GetClassObjects(ClassesManagerReborn.CardType.Card | ClassesManagerReborn.CardType.Entry | ClassesManagerReborn.CardType.SubClass | ClassesManagerReborn.CardType.Branch | ClassesManagerReborn.CardType.Gate).Select(co => co.card).Contains(card))
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
