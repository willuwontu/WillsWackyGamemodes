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
    /// A simple gamemode modifier that adds extra picks for all players.
    /// </summary>
    public static class ExtraStartingPicks
    {
        public static int extraPicks = 0;
        public const int maxExtraPicks = 5;
        public static bool pickHasRun = false;

        internal static IEnumerator StartingPicks(IGameModeHandler gm)
        {
            if (pickHasRun) { yield break; }

            //UnityEngine.Debug.Log("Running extra starting picks.");

            for (int i = 0; i < extraPicks; i++)
            {
                List<Player> pickOrder = PlayerManager.instance.GetPickOrder(null);

                foreach (Player player in pickOrder)
                {
                    yield return WillsWackyGameModes.instance.WaitForSyncUp();

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                    CardChoiceVisuals.instance.Show(player.playerID, true);
                    yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            pickHasRun = true;

            yield break;
        }
    }
}
