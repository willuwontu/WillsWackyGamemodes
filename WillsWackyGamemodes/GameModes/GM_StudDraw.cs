using RWF;
using RWF.GameModes;
using RWF.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.GameModes;
using UnityEngine;
using UnboundLib;
using WWGM.Algorithms;

namespace WWGM.GameModes
{
    /// <summary>
    /// A game mode which can be player as FFA or in teams. Similar to death match, players fight to see who the last player (or team) standing is.
    /// 
    /// Players pick a set number of cards at the start of the game, and then fight until they've won the game without anymore picks.
    /// </summary>
    public class GM_StudDraw : RWFGameMode
    {
        internal static GM_StudDraw instance;

        internal static int numOfPicks = 5;

        internal PickOrderStrategy currentStrategy;

        protected override void Awake()
        {
            GM_StudDraw.instance = this;
            this.currentStrategy = new DoubleBackStrategy();
            base.Awake();
        }

        public override IEnumerator DoStartGame()
        {
            CardBarHandler.instance.Rebuild();
            UIHandler.instance.InvokeMethod("SetNumberOfRounds", (int)GameModeManager.CurrentHandler.Settings["roundsToWinGame"]);
            ArtHandler.instance.NextArt();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameStart);

            GameManager.instance.battleOngoing = false;

            typeof(PickNCards.PickNCards).GetField("picks", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField | System.Reflection.BindingFlags.GetField).SetValue(null, 1);

            UIHandler.instance.ShowJoinGameText("Letsa go!", PlayerSkinBank.GetPlayerSkinColors(1).winText);
            yield return new WaitForSecondsRealtime(1f);
            UIHandler.instance.HideJoinGameText();

            PlayerSpotlight.CancelFade(true);

            PlayerManager.instance.SetPlayersSimulated(false);
            PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);
            MapManager.instance.LoadNextLevel(false, false);
            TimeHandler.instance.DoSpeedUp();

            yield return new WaitForSecondsRealtime(1f);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);

            this.currentStrategy = new DoubleBackStrategy();

            foreach (var player in PlayerManager.instance.players)
            {
                this.currentStrategy.AddPlayer(player);
            }

            List<Player> pickOrder;

            for (int i = 0; i < GM_StudDraw.numOfPicks; i++) 
            {
                pickOrder = this.currentStrategy.GetPickOrder(new int[] { });

                for (int j = 0; j < pickOrder.Count; j++)
                {
                    var player = pickOrder[j];

                    if (player == null) 
                    { 
                        continue; 
                    }

                    yield return this.WaitForSyncUp();

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                    CardChoiceVisuals.instance.Show(player.playerID, true);
                    yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            yield return this.WaitForSyncUp();
            CardChoiceVisuals.instance.Hide();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

            PlayerSpotlight.FadeIn();
            MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
            TimeHandler.instance.DoSpeedUp();
            TimeHandler.instance.StartGame();
            GameManager.instance.battleOngoing = true;
            UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);
            PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);

            this.StartCoroutine(this.DoRoundStart());
        }

        public override IEnumerator RoundTransition(int[] winningTeamIDs)
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundEnd);

            int[] winningTeams = GameModeManager.CurrentHandler.GetGameWinners();
            if (winningTeams.Any())
            {
                this.GameOver(winningTeamIDs);
                yield break;
            }

            this.StartCoroutine(PointVisualizer.instance.DoWinSequence(this.teamPoints, this.teamRounds, winningTeamIDs));

            yield return new WaitForSecondsRealtime(1f);
            MapManager.instance.LoadNextLevel(false, false);

            yield return new WaitForSecondsRealtime(1.3f);

            PlayerManager.instance.SetPlayersSimulated(false);
            TimeHandler.instance.DoSpeedUp();

            yield return this.StartCoroutine(this.WaitForSyncUp());
            PlayerSpotlight.FadeIn();

            TimeHandler.instance.DoSlowDown();
            MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
            PlayerManager.instance.RevivePlayers();

            yield return new WaitForSecondsRealtime(0.3f);

            TimeHandler.instance.DoSpeedUp();
            GameManager.instance.battleOngoing = true;
            this.isTransitioning = false;
            UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);

            this.StartCoroutine(this.DoRoundStart());
        }
    }
}
