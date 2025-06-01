namespace CarBlade.Core
{
    [System.Serializable]
    public class PlayerScore
    {
        public int playerId;
        public string playerName;
        public int score;
        public int kills;
        public int assists;
        public int deaths;

        public PlayerScore(int id, string name)
        {
            playerId = id;
            playerName = name;
            score = 0;
            kills = 0;
            assists = 0;
            deaths = 0;
        }
    }
}