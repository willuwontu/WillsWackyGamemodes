using RWF.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWGM.GameModes;

namespace WWGM.GameModeHandlers
{
    public class StudDraw : RWFGameModeHandler<GM_StudDraw>
    {
        internal const string GameModeName = "Stud Draw";
        internal const string GameModeID = "Stud";
        public StudDraw() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: false,
            pointsToWinRound: 3,
            roundsToWinGame: 3,
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: $"Only draw cards at the beginning of the game and then compete to see who the best player is."
            )
        {

        }
    }

    public class TeamStudDraw : RWFGameModeHandler<GM_StudDraw>
    {
        internal const string GameModeName = "Team Stud Draw";
        internal const string GameModeID = "Team Stud";
        public TeamStudDraw() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: true,
            pointsToWinRound: 3,
            roundsToWinGame: 3,
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: $"Only draw cards at the beginning of the game and then compete to see who the best team is."
            )
        {

        }
    }
}
