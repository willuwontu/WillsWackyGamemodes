//using Photon.Pun;
//using RWF;
//using RWF.GameModes;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnboundLib;
//using UnboundLib.GameModes;
//using UnboundLib.Networking;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using WWGM.Extensions;

//namespace WWGM.GameModeModifiers
//{
//    /// <summary>
//    /// A simple gamemode modifier that adds extra picks for all players.
//    /// </summary>
//    public static class MaxCards
//    {
//        internal const string ConfigSection = "Modifiers.MaxCards";

//        public static int maxCards = 5;
//        public static List<CardCategory> nonremovableCards = new List<CardCategory>() { 
//            CardChoiceSpawnUniqueCardPatch.CustomCategories.CustomCardCategories.instance.CardCategory("Curse"), 
//            CardChoiceSpawnUniqueCardPatch.CustomCategories.CustomCardCategories.instance.CardCategory("IgnoreMaxCardLimit")
//        };
//        public static bool enabled = false;

//        public static void RegisterIgnoredCategory(CardCategory cardCategory)
//        {
//            nonremovableCards.Add(cardCategory);
//        }

//        internal static IEnumerator CheckPlayerCards(IGameModeHandler gm)
//        {
//            if (pickHasRun) { yield break; }

//            //UnityEngine.Debug.Log("Running extra starting picks.");

//            for (int i = 0; i < extraPicks; i++)
//            {
//                List<Player> pickOrder = PlayerManager.instance.GetPickOrder(null);

//                foreach (Player player in pickOrder)
//                {
//                    yield return WillsWackyGameModes.instance.WaitForSyncUp();

//                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

//                    CardChoiceVisuals.instance.Show(player.playerID, true);
//                    yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);

//                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

//                    yield return new WaitForSecondsRealtime(0.1f);
//                }
//            }

//            pickHasRun = true;

//            yield break;
//        }
//    }

//    internal class Selectable : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
//    {
//        internal Player player;
//        int idx;
//        bool hover = false;
//        bool down = false;
//        Color orig;
//        Vector3 origScale;
//        void Start()
//        {
//            orig = ModdingUtils.Utils.CardBarUtils.instance.GetCardSquareColor(this.gameObject.transform.GetChild(0).gameObject);
//            origScale = this.gameObject.transform.localScale;
//            idx = this.gameObject.transform.GetSiblingIndex();
//        }
//        void Update()
//        {
//            idx = this.gameObject.transform.GetSiblingIndex();
//        }
//        public void OnPointerDown(PointerEventData eventData)
//        {
//            down = true;
//            this.gameObject.transform.localScale = Vector3.one;
//            Color.RGBToHSV(ModdingUtils.Utils.CardBarUtils.instance.GetCardSquareColor(this.gameObject.transform.GetChild(0).gameObject), out float h, out float s, out float v);
//            Color newColor = Color.HSVToRGB(h, s - 0.1f, v - 0.1f);
//            newColor.a = orig.a;
//            ModdingUtils.Utils.CardBarUtils.instance.ChangeCardSquareColor(this.gameObject.transform.GetChild(0).gameObject, newColor);
//        }
//        public void OnPointerUp(PointerEventData eventData)
//        {
//            if (down)
//            {
//                down = false;

//                this.gameObject.transform.localScale = origScale;
//                ModdingUtils.Utils.CardBarUtils.instance.ChangeCardSquareColor(this.gameObject.transform.GetChild(0).gameObject, orig);

//                if (hover)
//                {
//                    if (!PhotonNetwork.OfflineMode)
//                    {
//                        NetworkingManager.RPC(typeof(Selectable), nameof(RPCA_RemoveCardOnClick), new object[] { player.playerID, idx - 1 });
//                    }
//                    else
//                    {
//                        ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, idx - 1);
//                    }
//                }
//            }
//        }
//        public void OnPointerEnter(PointerEventData eventData)
//        {
//            hover = true;
//        }
//        public void OnPointerExit(PointerEventData eventData)
//        {
//            hover = false;
//        }
//        void OnDestroy()
//        {
//            this.gameObject.transform.localScale = origScale;
//            ModdingUtils.Utils.CardBarUtils.instance.ChangeCardSquareColor(this.gameObject.transform.GetChild(0).gameObject, orig);
//        }
//        [UnboundRPC]
//        private static void RPCA_RemoveCardOnClick(int playerID, int idx)
//        {
//            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer((Player)PlayerManager.instance.GetPlayerWithID(playerID), idx);
//        }
//    }
//}
