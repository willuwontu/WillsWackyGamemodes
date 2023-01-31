using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using UnboundLib;
using UnboundLib.Utils.UI;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using Jotunn.Utils;
using UnboundLib.Networking;
using HarmonyLib;
using Photon.Pun;
using TMPro;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnityEngine.UI;
using WWGM.GameModes;
using WWGM.GameModeHandlers;
using WWGM.GameModeModifiers;
using WWGM.Patches;
using RWF.GameModes;

namespace WWGM
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.pickncards", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("io.olavim.rounds.rwf", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class WillsWackyGameModes : BaseUnityPlugin
    {
        private const string ModId = "com.willuwontu.rounds.gamemodes";
        private const string ModName = "Will's Wacky GameModes";
        private const string ModConfigName = "WillsWackyGameModes";
        public const string Version = "0.0.2"; // What version are we on (major.minor.patch)?

        public const string ModInitials = "WWWGM";

        public static WillsWackyGameModes instance { get; private set; }

        public AssetBundle WWGMAssets { get; private set; }

        void Awake()
        {
            instance = this;

            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            RoundEndHandler_Patch.Initialize(harmony);
        }
        void Start()
        {
            Unbound.RegisterCredits(ModName, new string[] { "willuwontu" }, new string[] { "github", "Ko-Fi" }, new string[] { "https://github.com/willuwontu/wills-wacky-cards", "https://ko-fi.com/willuwontu" });

            Unbound.RegisterHandshake(WillsWackyGameModes.ModId, this.OnHandShakeCompleted);

            ConfigManager.Setup(this);

            GameModeManager.AddHandler<GM_StudDraw>(StudDraw.GameModeID, new StudDraw());
            GameModeManager.AddHandler<GM_StudDraw>(TeamStudDraw.GameModeID, new TeamStudDraw());
            GameModeManager.AddHandler<GM_RollingCardBar>(RollingCardBar.GameModeID, new RollingCardBar());
            GameModeManager.AddHandler<GM_RollingCardBar>(TeamRollingCardBar.GameModeID, new TeamRollingCardBar());
            GameModeManager.AddHandler<GM_Draft>(Draft.GameModeID, new Draft());
            GameModeManager.AddHandler<GM_Draft>(TeamDraft.GameModeID, new TeamDraft());
            ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(GM_Draft.Condition);
            ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(SingletonModifier.Condition);

            Unbound.RegisterMenu(ModName, () => { }, NewGUI, null, false);

            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, WinnersNeedHugsToo.WinnerPicks);
        }

        internal static IEnumerator GameStart(IGameModeHandler gm)
        {
            GameModeManager.AddOnceHook(GameModeHooks.HookPickStart, GameModeModifiers.ExtraStartingPicks.StartingPicks);

            yield break;
        }

        #region Handshake

        private void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(WillsWackyGameModes), nameof(SyncSettings), new object[] {
                    ConfigManager.studDraws.Value,
                    ConfigManager.rollingMaxCards.Value,
                    ConfigManager.draftExtraCards.Value,
                    ConfigManager.draftStartingPicks.Value,
                    ConfigManager.draftPicksPerRound.Value,
                    ConfigManager.draftCanDrawBetweenRounds.Value,
                    ConfigManager.draftPicksPerContinue.Value,
                    ConfigManager.draftDrawOnContinue.Value,
                    ConfigManager.draftContinueOwnCount.Value,

                    ConfigManager.extraStartingPicks.Value,
                    ConfigManager.singletonEnabled.Value,
                    ConfigManager.winnerHugsEnabled.Value
                });
            }
        }

        [UnboundRPC]
        private static void SyncSettings(int studDraws, int rollingMax, int draftExtra, int draftStarting, int draftRoundPicks, bool draftRoundPick, int draftContinuePicks, bool draftContinuePick, bool continueOwnCount, int extraStarting, bool singletonEnabled, bool winnerHugs)
        {
            GM_StudDraw.numOfPicks = studDraws;
            GM_RollingCardBar.maxAllowedCards = rollingMax;
            GM_Draft.startingPicks = draftStarting;
            GM_Draft.extraCardsDrawn = draftExtra;
            GM_Draft.picksPerRound = draftRoundPicks;
            GM_Draft.drawBetweenRounds = draftRoundPick;
            GM_Draft.drawOnContinue = draftContinuePick;
            GM_Draft.picksOnContinue = draftContinuePicks;
            GM_Draft.continueUsesOwnCount = continueOwnCount;

            ExtraStartingPicks.extraPicks = extraStarting;
            SingletonModifier.enabled = singletonEnabled;
            WinnersNeedHugsToo.enabled = winnerHugs;
        }

        #endregion Handshake

        #region IenumeratorSync

        [UnboundRPC]
        public static void RPC_RequestSync(int requestingPlayer)
        {
            NetworkingManager.RPC(typeof(WillsWackyGameModes), nameof(WillsWackyGameModes.instance.RPC_SyncResponse), requestingPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void RPC_SyncResponse(int requestingPlayer, int readyPlayer)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == requestingPlayer)
            {
                WillsWackyGameModes.instance.RemovePendingRequest(readyPlayer, nameof(WillsWackyGameModes.RPC_RequestSync));
            }
        }

        internal virtual IEnumerator WaitForSyncUp()
        {
            if (PhotonNetwork.OfflineMode)
            {
                yield break;
            }
            yield return this.SyncMethod(nameof(WillsWackyGameModes.instance.RPC_RequestSync), null, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        #endregion IenumeratorSync

        private static class ConfigManager
        {
            // Gamemode settings
            public static ConfigEntry<int> studDraws;
            public static ConfigEntry<int> rollingMaxCards;
            public static ConfigEntry<int> draftExtraCards;
            public static ConfigEntry<int> draftStartingPicks;
            public static ConfigEntry<int> draftPicksPerRound;
            public static ConfigEntry<bool> draftCanDrawBetweenRounds;
            public static ConfigEntry<int> draftPicksPerContinue;
            public static ConfigEntry<bool> draftDrawOnContinue;
            public static ConfigEntry<bool> draftContinueOwnCount;

            // Gamemode modifier settings
            public static ConfigEntry<int> extraStartingPicks;
            public static ConfigEntry<bool> singletonEnabled;
            public static ConfigEntry<bool> winnerHugsEnabled;

            public static void Setup(BaseUnityPlugin mod)
            {
                studDraws = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "StudDraws", 5, "Total number of pick phases before fighting starts.");
                rollingMaxCards = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "RollingMax", 5, "Maximum amount of cards a player can have in Rolling Cardbar matches.");
                draftExtraCards = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftExtraCards", 0, "The amount of cards over the number of picks to spawn in draft mode.");
                draftStartingPicks = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftStartingPicks", 3, "The number of pick phases at the start of draft mode.");
                draftPicksPerRound = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftPicksPerRound", 1, "The number of pick phases between each round in draft mode.");
                draftCanDrawBetweenRounds = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftCanDrawEachRound", true, "Whether pick phases occur between rounds in draft mode.");
                draftPicksPerContinue = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftPicksPerContinue", 1, "The number of pick phases when you continue.");
                draftDrawOnContinue = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftCanDrawOnContinue", true, "Whether pick phases occur on continue.");
                draftContinueOwnCount = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftContinueOwnCount", false, "Whether picking during a continue uses its own method to determine the amount of cards drawn.");

                GM_StudDraw.numOfPicks = studDraws.Value;
                GM_RollingCardBar.maxAllowedCards = rollingMaxCards.Value;
                GM_Draft.startingPicks = draftStartingPicks.Value;
                GM_Draft.extraCardsDrawn = draftExtraCards.Value;
                GM_Draft.picksPerRound = draftPicksPerRound.Value;
                GM_Draft.drawBetweenRounds = draftCanDrawBetweenRounds.Value;
                GM_Draft.drawOnContinue = draftDrawOnContinue.Value;
                GM_Draft.picksOnContinue = draftPicksPerContinue.Value;
                GM_Draft.continueUsesOwnCount = draftContinueOwnCount.Value;

                extraStartingPicks = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "ExtraStartingPicks", 0, "The number of extra pick phases at the start of a game.");
                singletonEnabled = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "SingletonEnabled", false, "Whether the singleton modifier is enabled.");
                winnerHugsEnabled = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "WinnerHugsEnabled", false, "Whether winners get to pick too.");

                ExtraStartingPicks.extraPicks = extraStartingPicks.Value;
                SingletonModifier.enabled = singletonEnabled.Value;
                WinnersNeedHugsToo.enabled = winnerHugsEnabled.Value;
            }
        }

        #region GUI

        private static void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            GameObject gamemodesMenu = MenuHandler.CreateMenu("Gamemodes", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            GamemodesMenu(gamemodesMenu);
            GameObject gamemodeModifiersMenu = MenuHandler.CreateMenu("General Modifiers", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            ModifiersMenu(gamemodeModifiersMenu);
        }

        #region GamemodeGUI

        private static void GamemodesMenu(GameObject menu)
        {
            MenuHandler.CreateText("Gamemodes", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            GameObject studMenu = MenuHandler.CreateMenu("Stud Draw", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            StudMenu(studMenu);
            GameObject rollingCardBarMenu = MenuHandler.CreateMenu("Rolling Cardbar", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            RollingCardBarMenu(rollingCardBarMenu);
            GameObject draftMenu = MenuHandler.CreateMenu("Draft Pick", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            DraftMenu(draftMenu);
        }

        private static void StudMenu(GameObject menu)
        {
            MenuHandler.CreateText("Stud Draw Options", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void CardsDrawn(float val)
            {
                ConfigManager.studDraws.Value = (int)val;
                GM_StudDraw.numOfPicks = (int)val;
            }
            MenuHandler.CreateSlider("Cards Drawn", menu, 30, 0f, 50f, ConfigManager.studDraws.Value, CardsDrawn, out Slider slider1, true);
        }

        private static void RollingCardBarMenu(GameObject menu)
        {
            MenuHandler.CreateText("Rolling Cardbar Options", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void MaxCardsAllowed(float val)
            {
                ConfigManager.rollingMaxCards.Value = (int)val;
                GM_RollingCardBar.maxAllowedCards= (int)val;
            }
            MenuHandler.CreateSlider("Maximum Cards", menu, 30, 1f, 10f, ConfigManager.rollingMaxCards.Value, MaxCardsAllowed, out Slider slider1, true);
        }

        private static void DraftMenu(GameObject menu)
        {
            MenuHandler.CreateText("Draft Options", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void InitialPicksChanged(float val)
            {
                ConfigManager.draftStartingPicks.Value = (int)val;
                GM_Draft.startingPicks= (int)val;
            }
            MenuHandler.CreateSlider("Starting Picks", menu, 30, 0f, 10f, ConfigManager.draftStartingPicks.Value, InitialPicksChanged, out Slider slider2, true);
            void ExtraCardsDrawnChanged(float val)
            {
                ConfigManager.draftExtraCards.Value = (int)val;
                GM_Draft.extraCardsDrawn = (int)val;
            }
            MenuHandler.CreateSlider("Extra Cards Drawn", menu, 30, 0f, 10f, ConfigManager.draftExtraCards.Value, ExtraCardsDrawnChanged, out Slider slider1, true);
            var canPickRoundObj = MenuHandler.CreateToggle(ConfigManager.draftCanDrawBetweenRounds.Value, "Can Pick Cards Each Round", menu, null, 30);
            var canPickRoundToggle = canPickRoundObj.GetComponent<Toggle>();
            void PicksPerRoundChanged(float val)
            {
                ConfigManager.draftPicksPerRound.Value = (int)val;
                GM_Draft.picksPerRound = (int)val;
            }
            var roundPicksObj = MenuHandler.CreateSlider("Picks Per Round", menu, 30, 0f, 5f, ConfigManager.draftPicksPerRound.Value, PicksPerRoundChanged, out Slider slider3, true);
            var canPickContinueObj = MenuHandler.CreateToggle(ConfigManager.draftDrawOnContinue.Value, "Can Pick Cards On Continue", menu, null, 30);
            var canPickContinueToggle = canPickContinueObj.GetComponent<Toggle>();
            void PicksPerContinueChanged(float val)
            {
                ConfigManager.draftPicksPerContinue.Value = (int)val;
                GM_Draft.picksOnContinue = (int)val;
            }
            var continuePicksObj = MenuHandler.CreateSlider("Picks Per Continue", menu, 30, 0f, 5f, ConfigManager.draftPicksPerContinue.Value, PicksPerContinueChanged, out Slider slider4, true);
            void ContinueOwnCountChanged(bool val)
            {
                ConfigManager.draftContinueOwnCount.Value = val;
                GM_Draft.continueUsesOwnCount = val;
            }
            var continueUsesOwnCountObj = MenuHandler.CreateToggle(ConfigManager.draftContinueOwnCount.Value, "Recalculate Continue Hand Size", menu, ContinueOwnCountChanged, 30);

            roundPicksObj.SetActive(ConfigManager.draftCanDrawBetweenRounds.Value);
            canPickContinueObj.SetActive(!ConfigManager.draftCanDrawBetweenRounds.Value);
            continuePicksObj.SetActive(ConfigManager.draftDrawOnContinue.Value && !ConfigManager.draftCanDrawBetweenRounds.Value);
            continueUsesOwnCountObj.SetActive(ConfigManager.draftDrawOnContinue.Value && !ConfigManager.draftCanDrawBetweenRounds.Value);
            canPickRoundToggle.onValueChanged.AddListener((val) =>
            {
                ConfigManager.draftCanDrawBetweenRounds.Value = val;
                GM_Draft.drawBetweenRounds = val;

                roundPicksObj.SetActive(val);
                canPickContinueObj.SetActive(!val);
                continuePicksObj.SetActive(!val);
                continueUsesOwnCountObj.SetActive(!val);
                if (!val)
                {
                    continuePicksObj.SetActive(ConfigManager.draftDrawOnContinue.Value);
                    continueUsesOwnCountObj.SetActive(ConfigManager.draftDrawOnContinue.Value);
                }
            });
            canPickContinueToggle.onValueChanged.AddListener((val) =>
            {
                ConfigManager.draftDrawOnContinue.Value = val;
                GM_Draft.drawOnContinue = val;
                continuePicksObj.SetActive(val);
                continueUsesOwnCountObj.SetActive(val);
            });
        }

        #endregion GamemodeGUI

        #region ModifierGUI

        private static void ModifiersMenu(GameObject menu)
        {
            MenuHandler.CreateText("Modifiers", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            GameObject startingPicksMenu = MenuHandler.CreateMenu("Starting Picks", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            StartingPicksMenu(startingPicksMenu);
            GameObject stingletonMenu = MenuHandler.CreateMenu("Singleton", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            SingletonMode(stingletonMenu);
            GameObject winnerHugsMenu = MenuHandler.CreateMenu("Winners Need Hugs Too", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            WinnerHugs(winnerHugsMenu);
        }

        private static void StartingPicksMenu(GameObject menu)
        {
            MenuHandler.CreateText("Extra Starting Picks", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void ChangedStartingPicks(float val)
            {
                ConfigManager.extraStartingPicks.Value = (int)val;
                ExtraStartingPicks.extraPicks = (int)val;
            }
            MenuHandler.CreateSlider("Extra Picks", menu, 30, 0f, ExtraStartingPicks.maxExtraPicks, ConfigManager.extraStartingPicks.Value, ChangedStartingPicks, out Slider slider1, true);
        }

        private static void SingletonMode(GameObject menu)
        {
            MenuHandler.CreateText("Singleton Mode", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText("If this modifier is enabled, players are prevented from receiving cards that someone already has.", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void ChangedSingletonEnabled(bool val)
            {
                ConfigManager.singletonEnabled.Value = val;
                SingletonModifier.enabled = val;
            }
            var continueUsesOwnCountObj = MenuHandler.CreateToggle(ConfigManager.singletonEnabled.Value, "Enabled", menu, ChangedSingletonEnabled, 30);
        }

        private static void WinnerHugs(GameObject menu)
        {
            MenuHandler.CreateText("Winners Need Hugs Too", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText("If this modifier is enabled, winners get to pick cards as well.", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void ChangedWinnerHugsEnabled(bool val)
            {
                ConfigManager.winnerHugsEnabled.Value = val;
                WinnersNeedHugsToo.enabled = val;
            }
            var continueUsesOwnCountObj = MenuHandler.CreateToggle(ConfigManager.winnerHugsEnabled.Value, "Enabled", menu, ChangedWinnerHugsEnabled, 30);
        }

        #endregion ModifierGUI

        #endregion GUI
    }
}
