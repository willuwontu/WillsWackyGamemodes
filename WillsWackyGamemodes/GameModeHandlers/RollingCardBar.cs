using RWF.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWGM.GameModes;

namespace WWGM.GameModeHandlers
{
    public class RollingCardBar : RWFGameModeHandler<GM_RollingCardBar>
    {
        internal const string GameModeName = "Rolling Cardbar";
        internal const string GameModeID = "Rolling Cardbar";
        public RollingCardBar() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: false,
            pointsToWinRound: 2,
            roundsToWinGame: 3,
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: $"Players can only hold a limited number of cards, with new ones pushing out the old.\n\nIf playing with Classes Manager Reborn, it is recommended that force classes is turned off."
            )
        {

        }
    }

    public class TeamRollingCardBar : RWFGameModeHandler<GM_RollingCardBar>
    {
        internal const string GameModeName = "Team Rolling Cardbar";
        internal const string GameModeID = "Team Rolling Cardbar";
        public TeamRollingCardBar() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: true,
            pointsToWinRound: 2,
            roundsToWinGame: 5,
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: $"Players can only hold a limited number of cards, with new ones pushing out the old.\n\nIf playing with Classes Manager Reborn, it is recommended that force classes is turned off."
            )
        {

        }
    }
}
