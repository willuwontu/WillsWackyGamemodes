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
using RWF;
using System.Reflection;

namespace WWGM.Patches
{
    class RoundEndHandler_Patch
    {
        public static void Initialize(Harmony harmony)
        {
            var types = AccessTools.GetTypesFromAssembly(typeof(RWF.RWFMod).Assembly);

            Type RoundEndHandler = null;

            foreach (var type in types)
            {
                if (type.Name == "RoundEndHandler")
                {
                    RoundEndHandler = type;
                }
            }

            MethodBase continueMethod = RoundEndHandler.GetMethod("Continue", BindingFlags.Default | BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod);
            harmony.Patch(continueMethod, new HarmonyMethod(typeof(RoundEndHandler_Patch), nameof(RoundEndHandler_Patch.CheckForContinue)));
        }

        [HarmonyPrefix]
        [HarmonyPatch("Continue")]
        static void CheckForContinue()
        {
            if (!(GameModeManager.CurrentHandlerID == Draft.GameModeID || GameModeManager.CurrentHandlerID == TeamDraft.GameModeID))
            {
                return;
            }

            GM_Draft.continuing = true;
        }

        //[HarmonyPostfix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}
    }
}