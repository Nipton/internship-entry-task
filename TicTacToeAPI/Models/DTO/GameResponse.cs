using System.Text.Json.Serialization;

namespace TicTacToeAPI.Models.DTO
{
    public class GameResponse
    {
        public Guid Id { get; set; }
        public string PlayerX { get; set; } = string.Empty;
        public string PlayerO { get; set; } = string.Empty;
        public string CurrentPlayer { get; set; } = string.Empty;
        public required char[][] Board { get; set; }
        public int BoardSize { get; set; }
        public int WinLineLength { get; set; }
        public int MoveCount { get; set; }
        public GameStatus Status { get; set; }
        [JsonIgnore]
        public string ETag { get; set; } = string.Empty;
    }
}
