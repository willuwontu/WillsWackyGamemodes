using MapEmbiggener.Controllers;
using MapEmbiggener.UI;
using MapEmbiggener;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.GameModes;
using UnityEngine;

namespace WWGM.Controllers
{
    public class ConditionHandler
    {
        #region Configs

        public const string SuddenDeathConfigSection = "Modifiers.MapBorders.SuddenDeath";

        public static Config<bool> suddenDeathEnabled;

        public static Config<bool> zoomByTimer;
        public static Config<float> suddenDeathCountdown;
        public static Config<float> minimumZoom;


        public static Config<int> minimumPlayers;

        public static Config<bool> zoomOnPlayerDeath;

        public static void Setup()
        {
            suddenDeathEnabled = ConfigManager.Bind<bool>(SuddenDeathConfigSection, "Enabled", false, "Whether the Sudden Death Borders are enabled.");
            zoomByTimer = ConfigManager.Bind<bool>(SuddenDeathConfigSection, "ZoomOnTimer", false, "Whether the map starts zooming in after a set time.");
            zoomOnPlayerDeath = ConfigManager.Bind<bool>(SuddenDeathConfigSection, "ZoomOnDeath", false, "Whether the map zooms in based on the ratio of dead to alive players.");

            suddenDeathCountdown = ConfigManager.Bind<float>(SuddenDeathConfigSection, "ZoomTimer", 120f, "The time it takes for the map to start zooming in.");
            minimumZoom = ConfigManager.Bind<float>(SuddenDeathConfigSection, "MinimumZoom", 0f, "The smallest that the map borders are shrunk down to.");

            minimumPlayers = ConfigManager.Bind<int>(SuddenDeathConfigSection, "MinimumPlayers", 2, "The numbers of players at which maximum zoom is enabled.");
        }

        #endregion Configs

        private int PlayersAlive => PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.playerID).Distinct().Count();
        private int TeamsAlive => PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.teamID).Distinct().Count();

        public bool CheckConditions()
        {
            return false;
        }
    }
}
