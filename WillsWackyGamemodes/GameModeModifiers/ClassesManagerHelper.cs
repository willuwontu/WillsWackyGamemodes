using BepInEx.Bootstrap;
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
using ClassesManagerReborn;

namespace WWGM.GameModeModifiers
{
    /// <summary>
    /// A class to help out with a soft dependency on CMR.
    /// </summary>
    public static class ClassesManagerHelper
    {
        public static bool IsClassCard(CardInfo card)
        {
            return ClassesRegistry.GetClassObjects(CardType.Card | CardType.Entry | CardType.SubClass | CardType.Branch | CardType.Gate).Select(co => co.card).Contains(card);
        }
    }
}
