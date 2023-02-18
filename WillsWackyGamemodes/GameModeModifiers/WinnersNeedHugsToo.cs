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
    /// A simple gamemode modifier that gives a .
    /// </summary>
    public static class WinnersNeedHugsToo
    {
        public static bool enabled;

        internal static IEnumerator WinnerPicks(IGameModeHandler gm)
        {
            if (!enabled)
            {
                yield break;
            }

            //UnityEngine.Debug.Log("Running winner picks");

            List<Player> winners = PlayerManager.instance.players.Where(p => GameModeManager.CurrentHandler.GetRoundWinners().Contains(p.teamID)).ToList();

            foreach (Player player in winners)
            {
                if (!PlayerManager.instance.players.Contains(player)) { continue; }

                yield return WillsWackyGameModes.instance.WaitForSyncUp();

                yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                CardChoiceVisuals.instance.Show(player.playerID, true);
                yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);

                yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                yield return new WaitForSecondsRealtime(0.1f);
            }

            yield break;
        }
    }
}
