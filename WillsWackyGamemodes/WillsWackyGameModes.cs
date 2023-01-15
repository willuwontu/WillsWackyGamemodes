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
using WWGM.Patches;

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
        public const string Version = "0.0.0"; // What version are we on (major.minor.patch)?

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

            Unbound.RegisterMenu(ModName, () => { }, NewGUI, null, false);
        }

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
                    ConfigManager.draftContinueOwnCount.Value
                });
            }
        }
        [UnboundRPC]
        private static void SyncSettings(int studDraws, int rollingMax, int draftExtra, int draftStarting, int draftRoundPicks, bool draftRoundPick, int draftContinuePicks, bool draftContinuePick, bool continueOwnCount)
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
        }

        private static class ConfigManager
        {
            public static ConfigEntry<int> studDraws;
            public static ConfigEntry<int> rollingMaxCards;
            public static ConfigEntry<int> draftExtraCards;
            public static ConfigEntry<int> draftStartingPicks;
            public static ConfigEntry<int> draftPicksPerRound;
            public static ConfigEntry<bool> draftCanDrawBetweenRounds;
            public static ConfigEntry<int> draftPicksPerContinue;
            public static ConfigEntry<bool> draftDrawOnContinue;
            public static ConfigEntry<bool> draftContinueOwnCount;

            public static void Setup(BaseUnityPlugin mod)
            {
                studDraws = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "StudDraws", 5, "Total number of pick phases before fighting starts.");
                rollingMaxCards = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "RollingMax", 5, "Maximum amount of cards a player can have in Rolling Cardbar matches.");
                draftExtraCards = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftExtraCards", 5, "The amount of cards over the number of players to spawn in draft mode.");
                draftStartingPicks = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftStartingPicks", 5, "The number of pick phases at the start of draft mode.");
                draftPicksPerRound = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftPicksPerRound", 5, "The number of pick phases between each round in draft mode.");
                draftCanDrawBetweenRounds = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftCanDrawEachRound", false, "Whether pick phases occur between rounds in draft mode.");
                draftPicksPerContinue = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftPicksPerContinue", 1, "The number of pick phases when you continue.");
                draftDrawOnContinue = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftCanDrawOnContinue", true, "Whether pick phases occur on continue.");
                draftContinueOwnCount = mod.Config.Bind(WillsWackyGameModes.ModConfigName, "DraftContinueOwnCount", false, "Whether picking during a continue uses its own method to determine the amount of cards drawn.");
            }
        }

        private static void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);
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
            }
            MenuHandler.CreateSlider("Starting Picks", menu, 30, 0f, 10f, ConfigManager.draftStartingPicks.Value, InitialPicksChanged, out Slider slider2, true);
            void ExtraCardsDrawnChanged(float val)
            {
                ConfigManager.draftExtraCards.Value = (int)val;
            }
            MenuHandler.CreateSlider("Extra Cards Drawn", menu, 30, 0f, 10f, ConfigManager.draftExtraCards.Value, ExtraCardsDrawnChanged, out Slider slider1, true);
            var canPickRoundObj = MenuHandler.CreateToggle(ConfigManager.draftCanDrawBetweenRounds.Value, "Can Pick Cards Each Round", menu, null, 30);
            var canPickRoundToggle = canPickRoundObj.GetComponent<Toggle>();
            void PicksPerRoundChanged(float val)
            {
                ConfigManager.draftPicksPerRound.Value = (int)val;
            }
            var roundPicksObj = MenuHandler.CreateSlider("Picks Per Round", menu, 30, 0f, 5f, ConfigManager.draftPicksPerRound.Value, PicksPerRoundChanged, out Slider slider3, true);
            var canPickContinueObj = MenuHandler.CreateToggle(ConfigManager.draftCanDrawBetweenRounds.Value, "Can Pick Cards On Continue", menu, null, 30);
            var canPickContinueToggle = canPickContinueObj.GetComponent<Toggle>();
            void PicksPerContinueChanged(float val)
            {
                ConfigManager.draftPicksPerContinue.Value = (int)val;
            }
            var continuePicksObj = MenuHandler.CreateSlider("Picks Per Continue", menu, 30, 0f, 5f, ConfigManager.draftPicksPerContinue.Value, PicksPerContinueChanged, out Slider slider4, true);
            void ContinueOwnCountChanged(bool val)
            {
                ConfigManager.draftContinueOwnCount.Value = val;
            }
            var continueUsesOwnCountObj = MenuHandler.CreateToggle(ConfigManager.draftCanDrawBetweenRounds.Value, "Recalculate Continue Hand Size", menu, ContinueOwnCountChanged, 30);

            roundPicksObj.SetActive(ConfigManager.draftCanDrawBetweenRounds.Value);
            continuePicksObj.SetActive(ConfigManager.draftDrawOnContinue.Value);
            continueUsesOwnCountObj.SetActive(ConfigManager.draftDrawOnContinue.Value);
            canPickRoundToggle.onValueChanged.AddListener((val) =>
            {
                ConfigManager.draftCanDrawBetweenRounds.Value = val;

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
                continuePicksObj.SetActive(val);
                continueUsesOwnCountObj.SetActive(val);
            });
        }
    }
}
