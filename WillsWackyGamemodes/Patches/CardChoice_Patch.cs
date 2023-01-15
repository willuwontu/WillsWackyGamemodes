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

namespace WWGM.Patches
{
    [HarmonyPatch(typeof(CardChoice))] 
    class CardChoice_Patch
    {
        public static List<GameObject> SpawnedCards => (List<GameObject>)CardChoice.instance.GetFieldValue("spawnedCards");

        class SimpleEnumerator : IEnumerable
        {
            public IEnumerator enumerator;
            public Action prefixAction, postfixAction;
            public Action<object> preItemAction, postItemAction;
            public Func<object, object> itemAction;
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
            public IEnumerator GetEnumerator()
            {
                prefixAction();
                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current;
                    preItemAction(item);
                    yield return itemAction(item);
                    postItemAction(item);
                }
                postfixAction();
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("ReplaceCards")]
        static void GetSpawnedCards(CardChoice __instance, List<GameObject> ___spawnedCards, ref IEnumerator __result)
        {
            Action prefixAction = () => {
                if (!(GameModeManager.CurrentHandlerID == Draft.GameModeID || GameModeManager.CurrentHandlerID == TeamDraft.GameModeID))
                {
                    return;
                }

                if (!(___spawnedCards.Count > 0))
                {
                    GM_Draft.spawningCards = true;
                }
            };
            Action<object> preItemAction = (item) => {  };
            Action<object> postItemAction = (item) => {  };
            Func<object, object> itemAction = (item) => { return item; };

            Action postfixAction = () => {
                if (!(GameModeManager.CurrentHandlerID == Draft.GameModeID || GameModeManager.CurrentHandlerID == TeamDraft.GameModeID))
                {
                    return;
                }

                GM_Draft.spawningCards = false;

                if (GM_Draft.firstpick && PlayerManager.instance.GetPlayerWithID(CardChoice.instance.pickrID) && (PhotonNetwork.OfflineMode || PlayerManager.instance.GetPlayerWithID(CardChoice.instance.pickrID).data.view.IsMine))
                {
                    List<string> cardNames = new List<string>();
                    foreach (var cardObj in ___spawnedCards)
                    {
                        var card = cardObj.GetComponent<CardInfo>();
                        if (card)
                        {
                            cardNames.Add(cardObj.name.Replace("(Clone)", ""));
                            //UnityEngine.Debug.Log(cardObj.name.Replace("(Clone)", ""));
                        }
                    }

                    NetworkingManager.RPC(typeof(GM_Draft), nameof(GM_Draft.URPCA_RegisterSpawnedCards), new object[] { CardChoice.instance.pickrID, cardNames.ToArray() });
                }
            };

            var myEnumerator = new SimpleEnumerator()
            {
                enumerator = __result,
                prefixAction = prefixAction,
                postfixAction = postfixAction,
                preItemAction = preItemAction,
                postItemAction = postItemAction,
                itemAction = itemAction
            };
            __result = myEnumerator.GetEnumerator();
        }

        [HarmonyPostfix]
        [HarmonyPatch("IDoEndPick")]
        static void GetPickedCard(CardChoice __instance, GameObject pickedCard, ref IEnumerator __result)
        {
            Action prefixAction = () =>
            {
                if (!(GameModeManager.CurrentHandlerID == Draft.GameModeID || GameModeManager.CurrentHandlerID == TeamDraft.GameModeID))
                {
                    return;
                }

                if (pickedCard != null && pickedCard.GetComponent<CardInfo>() && PlayerManager.instance.GetPlayerWithID(CardChoice.instance.pickrID) && (PhotonNetwork.OfflineMode || PlayerManager.instance.GetPlayerWithID(CardChoice.instance.pickrID).data.view.IsMine))
                {
                    //UnityEngine.Debug.Log("Sending URPCA");
                    UnboundLib.NetworkingManager.RPC(typeof(GM_Draft), nameof(GM_Draft.URPCA_RemoveCard), new object[] { CardChoice.instance.pickrID, pickedCard.name.Replace("(Clone)", "") });
                }
            };
            Action<object> preItemAction = (item) => { };
            Action<object> postItemAction = (item) => { };
            Func<object, object> itemAction = (item) => { return item; };

            Action postfixAction = () =>
            {

            };

            var myEnumerator = new SimpleEnumerator()
            {
                enumerator = __result,
                prefixAction = prefixAction,
                postfixAction = postfixAction,
                preItemAction = preItemAction,
                postItemAction = postItemAction,
                itemAction = itemAction
            };
            __result = myEnumerator.GetEnumerator();
        }

        //[HarmonyPrefix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}

        //[HarmonyPostfix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}
    }
}