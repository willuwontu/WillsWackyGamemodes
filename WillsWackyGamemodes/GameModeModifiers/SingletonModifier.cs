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

        public static bool Condition(Player player, CardInfo card)
        {
            if (!enabled)
            {
                return true;
            }

            if (!player || !player.data || !player.data.block || (player.data.currentCards == null))
            {
                return true;
            }

            if (PlayerManager.instance.players.Any(p => p.data.currentCards.Contains(card)))
            {
                return false;
            }

            return true;
        }
    }
}
