using RWF.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWGM.GameModes;

namespace WWGM.GameModeHandlers
{
    public class Draft : RWFGameModeHandler<GM_Draft>
    {
        internal const string GameModeName = "Draft";
        internal const string GameModeID = "Draft";
        public Draft() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: false,
            pointsToWinRound: 5,
            roundsToWinGame: 2,
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: $"Players pass their hands around to each other as they build their decks."
            )
        {

        }
    }

    public class TeamDraft : RWFGameModeHandler<GM_Draft>
    {
        internal const string GameModeName = "Team Draft";
        internal const string GameModeID = "Team Draft";
        public TeamDraft() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: true,
            pointsToWinRound: 5,
            roundsToWinGame: 2,
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: $"Players pass their hands around to each other as they build their decks."
            )
        {

        }
    }
}
