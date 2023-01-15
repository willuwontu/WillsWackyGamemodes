using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWGM.Algorithms
{
    public abstract class PickOrderStrategy
    {
        public abstract void AddPlayer(Player player);
        public abstract void RefreshOrder(int[] winningTeamIDs);
        public abstract IEnumerable<Player> GetPlayers(int[] winningTeamIDs);

        public List<Player> GetPickOrder(int[] winningTeamIDs)
        {
            var list = this.GetPlayers(winningTeamIDs).ToList();
            this.RefreshOrder(winningTeamIDs);
            return list;
        }
    }

    public class DoubleBackStrategy : PickOrderStrategy
    {
        private Dictionary<int, List<Player>> teamPlayers;
        private List<int> teamOrder;
        private bool doubleBacking;

        public DoubleBackStrategy()
        {
            this.teamPlayers = new Dictionary<int, List<Player>>();
            this.teamOrder = new List<int>();
            this.doubleBacking = false;
        }

        public override void RefreshOrder(int[] winningTeamIDs)
        {
            foreach (var key in this.teamPlayers.Keys)
            {
                this.teamPlayers[key].Reverse();
                if (doubleBacking)
                {
                    this.teamPlayers[key].Reverse();
                    if (!winningTeamIDs.Contains(key))
                    {
                        this.teamPlayers[key].Add(this.teamPlayers[key][0]);
                        this.teamPlayers[key].RemoveAt(0);
                        this.teamPlayers[key].Add(this.teamPlayers[key][0]);
                        this.teamPlayers[key].RemoveAt(0);
                    }
                }
                else
                {
                    this.teamPlayers[key].Reverse();
                }
            }

            this.teamOrder.Reverse();

            if (doubleBacking)
            {
                Dictionary<int, int> winnerIndices = winningTeamIDs.ToDictionary(team => team, team => this.teamOrder.IndexOf(team));
                
                for (int i = this.teamOrder.Count - 1; i >= 0; i--)
                {
                    if (winnerIndices.Values.Contains(i))
                    {
                        this.teamOrder.RemoveAt(i);
                    }
                }

                this.teamOrder.Add(this.teamOrder[0]);
                this.teamOrder.RemoveAt(0);
                this.teamOrder.Add(this.teamOrder[0]);
                this.teamOrder.RemoveAt(0);

                foreach (var team in winnerIndices.Keys.OrderBy(key => winnerIndices[key]))
                {
                    this.teamOrder.Add(team);
                }
            }

            this.doubleBacking = !this.doubleBacking;
        }

        public override void AddPlayer(Player player)
        {
            if (!this.teamOrder.Contains(player.teamID))
            {
                this.teamOrder.Add(player.teamID);
            }

            if (!this.teamPlayers.ContainsKey(player.teamID))
            {
                this.teamPlayers.Add(player.teamID, new List<Player>());
            }

            this.teamPlayers[player.teamID].Add(player);
        }

        public override IEnumerable<Player> GetPlayers(int[] winningTeamIDs)
        {
            int maxTeamPlayers = this.teamPlayers.Max(p => p.Value.Count);

            for (int playerIndex = 0; playerIndex < maxTeamPlayers; playerIndex++)
            {
                foreach (int teamID in this.teamOrder.Where(id => !winningTeamIDs.Contains(id)))
                {
                    var playerOrder = this.teamPlayers[teamID];
                    if (playerIndex < playerOrder.Count)
                    {
                        yield return playerOrder[playerIndex];
                    }
                }
            }
        }
    }

    public class NoRotationStrategy : PickOrderStrategy
    {
        private Dictionary<int, List<Player>> playerOrders;
        private List<int> teamOrder;

        public NoRotationStrategy()
        {
            this.playerOrders = new Dictionary<int, List<Player>>();
            this.teamOrder = new List<int>();
        }

        public override void AddPlayer(Player player)
        {
            if (!this.playerOrders.ContainsKey(player.teamID))
            {
                this.playerOrders.Add(player.teamID, new List<Player>());
                this.teamOrder.Add(player.teamID);
            }

            this.playerOrders[player.teamID].Add(player);
        }

        public override void RefreshOrder(int[] winningTeamIDs)
        {

        }

        public override IEnumerable<Player> GetPlayers(int[] winningTeamIDs)
        {
            int maxTeamPlayers = this.playerOrders.Max(p => p.Value.Count);

            for (int playerIndex = 0; playerIndex < maxTeamPlayers; playerIndex++)
            {
                foreach (int teamID in this.teamOrder.Where(id => !winningTeamIDs.Contains(id)))
                {
                    var playerOrder = this.playerOrders[teamID];
                    if (playerIndex < playerOrder.Count)
                    {
                        yield return playerOrder[playerIndex];
                    }
                }
            }
        }
    }
}
