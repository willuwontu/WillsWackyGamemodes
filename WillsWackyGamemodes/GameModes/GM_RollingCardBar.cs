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
using Sonigon;

namespace WWGM.GameModes
{
    /// <summary>
    /// A game mode which can be player as FFA or in teams. Similar to death match, players fight to see who the last player (or team) standing is.
    /// 
    /// The game functions like normal until someone reaches the maximum number of cards, then that player's new cards push the old ones out. Force classes is not recommended.
    /// </summary>
    public class GM_RollingCardBar : RWFGameMode
    {
        internal static GM_RollingCardBar instance;

        internal static Config<int> maxAllowedCards;

        internal PickOrderStrategy currentStrategy;

        internal const string ConfigSection = "GameModes.RollingCardBar";

        public static void Setup()
        {
            maxAllowedCards = ConfigManager.Bind<int>(ConfigSection, "MaxCards", 5, "Maximum amount of cards a player can have in Rolling Cardbar matches.");
        }

        protected override void Awake()
        {
            GM_RollingCardBar.instance = this;
            this.currentStrategy = new NoRotationStrategy();
            base.Awake();
        }

        public override IEnumerator DoRoundStart()
        {
            // Wait for MapManager to set all players to playing after map transition
            while (PlayerManager.instance.players.ToList().Any(p => !(bool)p.data.isPlaying))
            {
                yield return null;
            }

            foreach (Player player in PlayerManager.instance.players)
            {
                if (player.data.currentCards.Count() > GM_RollingCardBar.maxAllowedCards.CurrentValue)
                {
                    ModdingUtils.Utils.Cards.instance.RemoveCardsFromPlayer(player, Enumerable.Range(0, player.data.currentCards.Count() - GM_RollingCardBar.maxAllowedCards.CurrentValue).ToArray());
                }
            }

            //PlayerManager.instance.SetPlayersSimulated(false);
            yield return this.WaitForSyncUp();
            PlayerSpotlight.FadeOut();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);

            var sounds = GameObject.Find("/SonigonSoundEventPool");

            yield return this.SyncBattleStart();

            /*
            for (int i = 3; i >= 1; i--)
            {
                UIHandler.instance.DisplayRoundStartText($"{i}");
                SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, this.transform);
                yield return new WaitForSecondsRealtime(0.5f);
            }
            UIHandler.instance.DisplayRoundStartText("FIGHT");
            */
            SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, this.transform);
            PlayerManager.instance.SetPlayersSimulated(true);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

            this.ExecuteAfterSeconds(0.5f, () => {
                UIHandler.instance.HideRoundStartText();
            });
        }

        public override IEnumerator DoPointStart()
        {
            // Wait for MapManager to set all players to playing after map transition
            while (PlayerManager.instance.players.ToList().Any(p => !(bool)p.data.isPlaying))
            {
                yield return null;
            }

            foreach (Player player in PlayerManager.instance.players)
            {
                if (player.data.currentCards.Count() > GM_RollingCardBar.maxAllowedCards.CurrentValue)
                {
                    ModdingUtils.Utils.Cards.instance.RemoveCardsFromPlayer(player, Enumerable.Range(0, player.data.currentCards.Count() - GM_RollingCardBar.maxAllowedCards.CurrentValue).ToArray());
                }
            }

            //PlayerManager.instance.SetPlayersSimulated(false);
            yield return this.WaitForSyncUp();
            PlayerSpotlight.FadeOut();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);

            var sounds = GameObject.Find("/SonigonSoundEventPool");

            yield return this.SyncBattleStart();

            /*
            for (int i = 3; i >= 1; i--)
            {
                UIHandler.instance.DisplayRoundStartText($"{i}");
                SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, this.transform);
                yield return new WaitForSecondsRealtime(0.5f);
            }
            UIHandler.instance.DisplayRoundStartText("FIGHT");
            */
            SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, this.transform);
            PlayerManager.instance.SetPlayersSimulated(true);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

            this.ExecuteAfterSeconds(0.5f, () => {
                UIHandler.instance.HideRoundStartText();
            });
        }
    }
}
