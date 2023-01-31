using HarmonyLib;
using UnityEngine;
using Sonigon;
using UnboundLib;
using UnboundLib.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using Photon.Pun;
using UnboundLib.GameModes;
using WWGM.Extensions;
using WWGM.GameModeHandlers;
using WWGM.GameModes;
using Photon.Realtime;
using UnboundLib.Cards;

namespace WWGM.Patches
{
    [HarmonyPatch(typeof(ApplyCardStats))] 
    public static class ApplyCardStats_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("ApplyStats")]
        static void OnCardAdded(ApplyCardStats __instance, Player ___playerToUpgrade)
        {
            var player = ___playerToUpgrade.GetComponent<Player>();
            var gun = ___playerToUpgrade.GetComponent<Holding>().holdable.GetComponent<Gun>();
            var characterData = ___playerToUpgrade.GetComponent<CharacterData>();
            var healthHandler = ___playerToUpgrade.GetComponent<HealthHandler>();
            var gravity = ___playerToUpgrade.GetComponent<Gravity>();
            var block = ___playerToUpgrade.GetComponent<Block>();
            var gunAmmo = gun.GetComponentInChildren<GunAmmo>();
            var characterStatModifiers = player.GetComponent<CharacterStatModifiers>();

            CardInfo card = __instance.GetComponent<CardInfo>();

            for (int i = cardAddedActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    cardAddedActions[i](card);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Card Added Action threw an exception.");
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        private static List<Action<CardInfo>> cardAddedActions = new List<Action<CardInfo>>();

        public static void AddCardAddedAction(Action<CardInfo> action)
        {
            if (action != null)
            {
                cardAddedActions.Add(action);
            }
        }
    }
}