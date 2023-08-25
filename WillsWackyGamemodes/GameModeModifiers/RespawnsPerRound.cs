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
    /// A simple gamemode modifier that makes it so that extra lives only give them once per round instead of each point.
    /// </summary>
    public static class RespawnsPerRound
    {
        internal const string ConfigSection = "Modifiers.Respawns";

        public static bool enabled = false;

        private static Dictionary<HealthHandler, int> currentExtraLives = new Dictionary<HealthHandler, int>();

        internal static void RecordRemainingRespawns(HealthHandler healthHandler)
        {
            currentExtraLives[healthHandler] = healthHandler.GetComponent<CharacterStatModifiers>().remainingRespawns;
        }

        internal static void UpdateRemainingRespawns(HealthHandler healthHandler)
        {
            if (currentExtraLives.ContainsKey(healthHandler))
            {
                healthHandler.GetComponent<CharacterStatModifiers>().remainingRespawns = currentExtraLives[healthHandler];
            }
        }

        internal static IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            currentExtraLives.Clear();

            yield break;
        }
    }
}
