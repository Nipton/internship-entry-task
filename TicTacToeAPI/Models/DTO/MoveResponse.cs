using System.Text.Json.Serialization;

namespace TicTacToeAPI.Models.DTO
{
    public class MoveResponse
    {
        public char[][]? Board { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsGameOver { get; set; } 
        public string? Winner { get; set; }
        [JsonIgnore]
        public string ETag { get; set; } = string.Empty;
    }
}
