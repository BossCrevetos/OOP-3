namespace OOP_3.Server.Models
{
    public class GameSession
    {
        public string Player1Id { get; set; }
        public string Player2Id { get; set; }
        public string CurrentPlayerId { get; set; }
        public bool IsGameActive { get; set; }
        public string WinnerId { get; set; }

        public GameSession()
        {
            IsGameActive = false;
        }

        public void StartGame(string firstPlayerId)
        {
            IsGameActive = true;
            CurrentPlayerId = firstPlayerId;
        }

        public void SetActivePlayer(string playerId)
        {
            if (playerId == Player1Id || playerId == Player2Id)
            {
                CurrentPlayerId = playerId;
            }
        }

        public bool IsPlayerTurn(string playerId)
        {
            return IsGameActive && CurrentPlayerId == playerId;
        }

        public string GetCurrentPlayerId()
        {
            return CurrentPlayerId;
        }

        public void SwitchTurn()
        {
            if (CurrentPlayerId == Player1Id)
            {
                CurrentPlayerId = Player2Id;
            }
            else if (CurrentPlayerId == Player2Id)
            {
                CurrentPlayerId = Player1Id;
            }
        }

        public void EndGame(string winnerId)
        {
            IsGameActive = false;
            WinnerId = winnerId;
        }
    }
}