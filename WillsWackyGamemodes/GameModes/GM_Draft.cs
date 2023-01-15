using RWF;
using RWF.GameModes;
using RWF.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;
using UnboundLib;
using WWGM.Algorithms;
using DrawNCards;
using PickNCards;
using WWGM.GameModeHandlers;
using WWGM.Extensions;
using System.Reflection;
using UnboundLib.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using Photon.Pun;

namespace WWGM.GameModes
{
    /// <summary>
    /// A game mode which can be player as FFA or in teams. Similar to death match, players fight to see who the last player (or team) standing is.
    /// 
    /// Players pick a set number of cards at the start of the game, and then fight until they've won the game without anymore picks.
    /// </summary>
    public class GM_Draft : RWFGameMode
    {
        internal static GM_Draft instance;
        internal const int minimumCardsInHand = 2;

        internal static int extraCardsDrawn = 5;
        internal static int startingPicks = 5;
        internal static int picksPerRound = 1;
        internal static bool drawBetweenRounds = false;
        internal static int picksOnContinue = 2;
        internal static bool drawOnContinue = true;
        internal static bool continueUsesOwnCount = true;

        internal static Dictionary<Player, List<CardInfo>> draftingcards = new Dictionary<Player, List<CardInfo>>();
        internal static bool firstpick = false;
        internal static bool spawningCards = false;
        internal static bool continuing = false;

        internal PickOrderStrategy currentStrategy;

        internal static CardCategory handManipulation => CustomCardCategories.instance.CardCategory("handManipulation");
        internal static CardCategory handSizeManipulation => CustomCardCategories.instance.CardCategory("handSizeManipulation");

        List<Player> prevPickOrder = new List<Player>();

        internal static CardInfo nullCard => (CardInfo)typeof(CardChoiceSpawnUniqueCardPatch.CardChoiceSpawnUniqueCardPatch).GetField("NullCard", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default | BindingFlags.GetField).GetValue(null);

        protected override void Awake()
        {
            GM_Draft.instance = this;
            this.currentStrategy = new NoRotationStrategy();
            base.Awake();
        }

        [UnboundRPC]
        public static void URPCA_RemoveCard(int playerID, string cardName)
        {
            var player = PlayerManager.instance.GetPlayerWithID(playerID);

            if (!draftingcards.ContainsKey(player) || draftingcards[player].Count < 1)
            {
                return;
            }

            CardInfo card = null;

            if (CardManager.cards.Values.Select(c => c.cardInfo.name).Contains(cardName))
            {
                card = CardManager.cards.Values.Select(c => c.cardInfo).First(c => c.name == cardName);
            }
            else if (ModdingUtils.Utils.Cards.instance.HiddenCards.Select(c => c.name).Contains(cardName))
            {
                card = ModdingUtils.Utils.Cards.instance.HiddenCards.First(c => c.name == cardName);
            }
            else
            {
                card = nullCard;
            }

            draftingcards[player].Remove(card);
        }

        [UnboundRPC]
        public static void URPCA_RegisterSpawnedCards(int playerID, string[] cardNames)
        {
            var player = PlayerManager.instance.GetPlayerWithID(playerID);

            if (!draftingcards.ContainsKey(player))
            {
                draftingcards.Add(player, new List<CardInfo>());
            }

            if (player != null && draftingcards[player].Count < 1)
            {
                foreach (var cardName in cardNames)
                {
                    if (CardManager.cards.Values.Select(c => c.cardInfo.name).Contains(cardName))
                    {
                        draftingcards[player].Add(CardManager.cards.Values.Select(c => c.cardInfo).First(c => c.name == cardName));
                    }
                    else if (ModdingUtils.Utils.Cards.instance.HiddenCards.Select(c => c.name).Contains(cardName))
                    {
                        draftingcards[player].Add(ModdingUtils.Utils.Cards.instance.HiddenCards.First(c => c.name == cardName));
                    }
                    else
                    {
                        draftingcards[player].Add(nullCard);
                    }
                }
            }
        }

        internal static bool Condition(Player player, CardInfo card)
        {
            // Only apply during our gamemode
            if (!(GameModeManager.CurrentHandlerID == Draft.GameModeID || GameModeManager.CurrentHandlerID == TeamDraft.GameModeID))
            {
                return true;
            }

            // We don't apply if it's a fresh pick, or we're not spawning cards.
            if (GM_Draft.firstpick || !GM_Draft.spawningCards)
            {
                return true;
            }

            if (card.categories.Contains(GM_Draft.handManipulation) || card.categories.Contains(GM_Draft.handSizeManipulation))
            {
                return false;
            }

            // If it's not a card in the player's hand, skip it.
            if (!(draftingcards[player].Contains(card)))
            {
                return false;
            }

            return true;
        }

        private IEnumerator HandlePicks(int[] winningTeamIDs)
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);

            int[] winners = winningTeamIDs ?? new int[0];

            List<Player> pickOrder = PlayerManager.instance.GetPickOrder(new int[] { -1 });

            // get a list of all current handsizes and their max and min
            List<int> handSizes = draftingcards.Where(kvp => PlayerManager.instance.players.Contains(kvp.Key)).Select(kvp => kvp.Value.Count()).ToList();
            int min = 0;
            int max = 0;

            if (draftingcards.Count > 0)
            {
                min = handSizes.Min();
                max = handSizes.Max();
            }

            UnityEngine.Debug.Log($"Smallest Hand Size is {min}");

            // If we're under the minimum hand size, we flag for creating a new set of hands for players
            if (min < minimumCardsInHand) 
            {
                UnityEngine.Debug.Log($"Clearing hands");
                GM_Draft.draftingcards.Clear();
                GM_Draft.draftingcards = new Dictionary<Player, List<CardInfo>>();

                foreach (var player in PlayerManager.instance.players)
                {
                    GM_Draft.draftingcards[player] = new List<CardInfo>();
                }

                
                GM_Draft.firstpick = true;
                UnityEngine.Debug.Log($"First pick is {GM_Draft.firstpick}");
                min = GM_Draft.startingPicks + GM_Draft.extraCardsDrawn + 1;
                if (continuing && GM_Draft.continueUsesOwnCount)
                {
                    min = GM_Draft.picksOnContinue + GM_Draft.extraCardsDrawn + 1;
                }
            }
            else
            {
                // We check to make sure that our hands are all the same size
                if (min != max)
                {
                    // if they're not all the same size we loop through and randomly remove cards until they are.
                    foreach (var kvp in draftingcards)
                    {
                        while (kvp.Value.Count > min) 
                        {
                            UnityEngine.Debug.Log($"Clipping a card from Player {kvp.Key.playerID}'s hand.");
                            var ind = UnityEngine.Random.Range(0, kvp.Value.Count);
                            kvp.Value.RemoveAt(ind);
                        }
                    }
                }
            }

            UnityEngine.Debug.Log($"Min set to {min}");

            // If it's not a first pick, and there was a previous pick order, we pass the hands around.
            if (!GM_Draft.firstpick && this.prevPickOrder.Count > 0)
            {
                // We make sure we only have players that are present in the previous pick order.
                prevPickOrder = prevPickOrder.Intersect(pickOrder).ToList();
                var extraPlayers = pickOrder.Except(prevPickOrder).ToList();

                // Dictionary to temporarily hold the new hands.
                Dictionary<Player, List<CardInfo>> newHands = new Dictionary<Player, List<CardInfo>>();

                for (int i = 0; i < prevPickOrder.Count; i++)
                {
                    UnityEngine.Debug.Log($"Transferring cards from player {prevPickOrder[i].playerID} to player {pickOrder[i].playerID}:");
                    foreach (var c in draftingcards[prevPickOrder[i]])
                    {
                        UnityEngine.Debug.Log($"{c.cardName}");
                    }

                    newHands[pickOrder[i]] = draftingcards[prevPickOrder[i]];
                }

                foreach (var person in extraPlayers)
                {
                    newHands[person] = new List<CardInfo>();
                }

                draftingcards = newHands.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            for (int i = 0; i < pickOrder.Count(); i++)
            {
                Player player = pickOrder[i];

                if (player == null)
                {
                    continue;
                }

                DrawNCards.DrawNCards.RPCA_SetPickerDraws(player.playerID, min);

                if (!winners.Contains(player.teamID))
                {
                    yield return this.WaitForSyncUp();

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                    CardChoiceVisuals.instance.Show(player.playerID, true);
                    yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                    yield return new WaitForSecondsRealtime(0.2f);
                }
                else if (firstpick)
                {
                    UnityEngine.Debug.Log($"Firstpick, Generating cards for a winner.");
                    if (player.data.view.IsMine || PhotonNetwork.OfflineMode)
                    {
                        List<CardInfo> newHand = new List<CardInfo>();
                        
                        for (int j = 0; j < min; j++)
                        {
                            CardInfo card = null;

                            try
                            {
                                card = ModdingUtils.Patches.CardChoicePatchGetRanomCard.EfficientGetRanomCard(player, CardChoice.instance.cards.Except(newHand).ToArray()).GetComponent<CardInfo>();
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogException(e);
                                card = nullCard;
                            }

                            newHand.Add(card);
                        }

                        foreach (var card in newHand)
                        {
                            UnityEngine.Debug.Log(card.name);
                        }

                        NetworkingManager.RPC(typeof(GM_Draft), nameof(GM_Draft.URPCA_RegisterSpawnedCards), new object[] { player.playerID, newHand.Select(c => c.name).ToArray() });
                    }
                }
            }

            GM_Draft.firstpick = false;
            this.prevPickOrder = pickOrder;

            yield return this.WaitForSyncUp();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

            CardChoiceVisuals.instance.Hide();

            yield break;
        }

        public override IEnumerator DoStartGame()
        {
            CardBarHandler.instance.Rebuild();
            UIHandler.instance.InvokeMethod("SetNumberOfRounds", (int)GameModeManager.CurrentHandler.Settings["roundsToWinGame"]);
            ArtHandler.instance.NextArt();

            typeof(PickNCards.PickNCards).GetField("picks", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField | System.Reflection.BindingFlags.GetField).SetValue(null, 1);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameStart);

            GameManager.instance.battleOngoing = false;

            UIHandler.instance.ShowJoinGameText("Drafting Time!", PlayerSkinBank.GetPlayerSkinColors(1).winText);
            yield return new WaitForSecondsRealtime(1f);
            UIHandler.instance.HideJoinGameText();

            PlayerSpotlight.CancelFade(true);

            PlayerManager.instance.SetPlayersSimulated(false);
            PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);
            MapManager.instance.LoadNextLevel(false, false);
            TimeHandler.instance.DoSpeedUp();

            GM_Draft.draftingcards.Clear();
            GM_Draft.draftingcards = new Dictionary<Player, List<CardInfo>>();
            this.prevPickOrder = new List<Player>();
            GM_Draft.continuing = false;

            yield return new WaitForSecondsRealtime(1f);

            for (int i = 0; i < GM_Draft.startingPicks; i++)
            {
                yield return HandlePicks(null);
                yield return this.WaitForSyncUp();
            }

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


            PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);

            if (GM_Draft.drawBetweenRounds)
            {
                for (int i = 0; i < GM_Draft.picksPerRound; i++)
                {
                    yield return HandlePicks(winningTeamIDs);
                    yield return this.WaitForSyncUp();
                }
            }
            else if (GM_Draft.drawOnContinue && GM_Draft.continuing)
            {
                GM_Draft.draftingcards.Clear();
                GM_Draft.draftingcards = new Dictionary<Player, List<CardInfo>>();

                for (int i = 0; i < GM_Draft.picksOnContinue; i++)
                {
                    yield return HandlePicks(null);
                    yield return this.WaitForSyncUp();
                }
                GM_Draft.continuing = false;
            }

            PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);
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
