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
using SettingsUI;
using BepInEx.Bootstrap;
using static System.Collections.Specialized.BitVector32;

namespace WWGM
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInDependency("pykess.rounds.plugins.moddingutils")]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch")]
    [BepInDependency("pykess.rounds.plugins.pickncards")]
    [BepInDependency("io.olavim.rounds.rwf")]
    [BepInDependency("com.willuwontu.rounds.rwfsettingsui")]
    [BepInDependency("root.classes.manager.reborn", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class WillsWackyGameModes : BaseUnityPlugin
    {
        internal const string ModId = "com.willuwontu.rounds.gamemodes";
        private const string ModName = "Will's Wacky GameModes";
        private const string ModConfigName = "WillsWackyGameModes";
        public const string Version = "0.0.6"; // What version are we on (major.minor.patch)?

        public const string ModInitials = "WWGM";

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

            ConfigManager.Setup();

            GameModeManager.AddHandler<GM_StudDraw>(StudDraw.GameModeID, new StudDraw());
            GameModeManager.AddHandler<GM_StudDraw>(TeamStudDraw.GameModeID, new TeamStudDraw());
            GameModeManager.AddHandler<GM_RollingCardBar>(RollingCardBar.GameModeID, new RollingCardBar());
            GameModeManager.AddHandler<GM_RollingCardBar>(TeamRollingCardBar.GameModeID, new TeamRollingCardBar());
            GameModeManager.AddHandler<GM_Draft>(Draft.GameModeID, new Draft());
            GameModeManager.AddHandler<GM_Draft>(TeamDraft.GameModeID, new TeamDraft());
            ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(GM_Draft.Condition);
            ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(SingletonModifier.Condition);

            Unbound.RegisterMenu(ModName, () => { }, NewGUI, null, false);
            SettingsUI.RWFSettingsUI.RegisterMenu(ModName, NewGUI);

            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, GameModeModifiers.ExtraStartingPicks.StartingPicks);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, WinnersNeedHugsToo.WinnerPicks);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, RespawnsPerRound.OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, RespawnsPerRound.OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, RespawnsPerRound.OnRoundStart);
        }

        internal static IEnumerator GameStart(IGameModeHandler gm)
        {
            ExtraStartingPicks.pickHasRun = false;

            yield break;
        }

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

        #region MainMenuGUI

        internal static void NewGUI(GameObject menu)
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
                GM_StudDraw.numOfPicks.ConfigValue = (int)val;
            }
            MenuHandler.CreateSlider("Cards Drawn", menu, 30, 0f, 50f, GM_StudDraw.numOfPicks.ConfigValue, CardsDrawn, out Slider slider1, true);
        }

        private static void RollingCardBarMenu(GameObject menu)
        {
            MenuHandler.CreateText("Rolling Cardbar Options", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void MaxCardsAllowed(float val)
            {
                GM_RollingCardBar.maxAllowedCards.ConfigValue = (int)val;
            }
            MenuHandler.CreateSlider("Maximum Cards", menu, 30, 1f, 10f, GM_RollingCardBar.maxAllowedCards.ConfigValue, MaxCardsAllowed, out Slider slider1, true);
        }

        private static void DraftMenu(GameObject menu)
        {
            MenuHandler.CreateText("Draft Options", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void InitialPicksChanged(float val)
            {
                GM_Draft.startingPicks.ConfigValue = (int)val;
            }
            MenuHandler.CreateSlider("Starting Picks", menu, 30, 0f, 10f, GM_Draft.startingPicks.ConfigValue, InitialPicksChanged, out Slider slider2, true);
            void ExtraCardsDrawnChanged(float val)
            {
                GM_Draft.extraCardsDrawn.ConfigValue = (int)val;
            }
            MenuHandler.CreateSlider("Extra Cards Drawn", menu, 30, 0f, 10f, GM_Draft.extraCardsDrawn.ConfigValue, ExtraCardsDrawnChanged, out Slider slider1, true);
            var canPickRoundObj = MenuHandler.CreateToggle(GM_Draft.drawBetweenRounds.ConfigValue, "Can Pick Cards Each Round", menu, null, 30);
            var canPickRoundToggle = canPickRoundObj.GetComponent<Toggle>();
            void PicksPerRoundChanged(float val)
            {
                GM_Draft.picksPerRound.ConfigValue = (int)val;
            }
            var roundPicksObj = MenuHandler.CreateSlider("Picks Per Round", menu, 30, 0f, 5f, GM_Draft.picksPerRound.ConfigValue, PicksPerRoundChanged, out Slider slider3, true);
            var canPickContinueObj = MenuHandler.CreateToggle(GM_Draft.drawOnContinue.ConfigValue, "Can Pick Cards On Continue", menu, null, 30);
            var canPickContinueToggle = canPickContinueObj.GetComponent<Toggle>();
            void PicksPerContinueChanged(float val)
            {
                GM_Draft.picksOnContinue.ConfigValue = (int)val;
            }
            var continuePicksObj = MenuHandler.CreateSlider("Picks Per Continue", menu, 30, 0f, 5f, GM_Draft.picksOnContinue.ConfigValue, PicksPerContinueChanged, out Slider slider4, true);
            void ContinueOwnCountChanged(bool val)
            {
                GM_Draft.continueUsesOwnCount.ConfigValue = val;
            }
            var continueUsesOwnCountObj = MenuHandler.CreateToggle(GM_Draft.continueUsesOwnCount.ConfigValue, "Recalculate Continue Hand Size", menu, ContinueOwnCountChanged, 30);

            roundPicksObj.SetActive(GM_Draft.drawBetweenRounds.ConfigValue);
            canPickContinueObj.SetActive(!GM_Draft.drawBetweenRounds.ConfigValue);
            continuePicksObj.SetActive(GM_Draft.drawOnContinue.ConfigValue && !GM_Draft.drawBetweenRounds.ConfigValue);
            continueUsesOwnCountObj.SetActive(GM_Draft.drawOnContinue.ConfigValue && !GM_Draft.drawBetweenRounds.ConfigValue);
            canPickRoundToggle.onValueChanged.AddListener((val) =>
            {
                GM_Draft.drawBetweenRounds.ConfigValue = val;

                roundPicksObj.SetActive(val);
                canPickContinueObj.SetActive(!val);
                continuePicksObj.SetActive(!val);
                continueUsesOwnCountObj.SetActive(!val);
                if (!val)
                {
                    continuePicksObj.SetActive(GM_Draft.drawOnContinue.ConfigValue);
                    continueUsesOwnCountObj.SetActive(GM_Draft.drawOnContinue.ConfigValue);
                }
            });
            canPickContinueToggle.onValueChanged.AddListener((val) =>
            {
                GM_Draft.drawOnContinue.ConfigValue = val;
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
            MenuHandler.CreateText("The modifiers below can be used with any gamemode, not just those present in Will's Wacky Gamemodes.", menu, out TextMeshProUGUI _, 30);
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
                ExtraStartingPicks.extraPicks.ConfigValue = (int)val;
            }
            MenuHandler.CreateSlider("Extra Picks", menu, 30, 0f, ExtraStartingPicks.maxExtraPicks, ExtraStartingPicks.extraPicks.ConfigValue, ChangedStartingPicks, out Slider slider1, true);
        }

        private static void SingletonMode(GameObject menu)
        {
            MenuHandler.CreateText("Singleton Mode", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText("If this modifier is enabled, players are prevented from receiving cards that someone already has.", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void ChangedSingletonEnabled(bool val)
            {
                SingletonModifier.enabled.ConfigValue = val;
            }
            var singletonEnabledObj = MenuHandler.CreateToggle(SingletonModifier.enabled.ConfigValue, "Enabled", menu, null, 30);
            var singletonEnabledToggle = singletonEnabledObj.GetComponentInChildren<Toggle>();

            void ChangedSelfEnabled(bool val)
            {
                SingletonModifier.selfEnabled.ConfigValue = val;
            }
            var selfEnabledObj = MenuHandler.CreateToggle(SingletonModifier.selfEnabled.ConfigValue, "Allow duplicates of cards you have.", menu, ChangedSelfEnabled, 30);
            Toggle selfEnabledToggle = selfEnabledObj.GetComponentInChildren<Toggle>();
            selfEnabledToggle.interactable = SingletonModifier.selfEnabled.ConfigValue;

            void ChangedClassEnabled(bool val)
            {
                SingletonModifier.classEnabled.ConfigValue = val;
            }
            GameObject classEnabledObj = null;
            Toggle classEnabledToggle = null;
            if (Chainloader.PluginInfos.Keys.Contains("root.classes.manager.reborn"))
            {
                classEnabledObj = MenuHandler.CreateToggle(SingletonModifier.classEnabled.ConfigValue, "Allow duplicates of class cards you have.", menu, ChangedClassEnabled, 30);
                classEnabledToggle = classEnabledObj.GetComponentInChildren<Toggle>();
                classEnabledToggle.interactable = SingletonModifier.enabled.ConfigValue;
            }

            singletonEnabledToggle.onValueChanged.AddListener((val) =>
            {
                ChangedSingletonEnabled(val);
                selfEnabledToggle.interactable = val;
                if (classEnabledToggle != null) 
                { 
                    classEnabledToggle.interactable = val; 
                }
            });
        }

        private static void WinnerHugs(GameObject menu)
        {
            MenuHandler.CreateText("Winners Need Hugs Too", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText("If this modifier is enabled, winners get to pick cards as well.", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void ChangedWinnerHugsEnabled(bool val)
            {
                WinnersNeedHugsToo.enabled.ConfigValue = val;
            }
            var winnerHugsEnabledObj = MenuHandler.CreateToggle(WinnersNeedHugsToo.enabled.ConfigValue, "Enabled", menu, ChangedWinnerHugsEnabled, 30);
        }

        #endregion ModifierGUI

        #endregion MainMenuGUI
    }
}
