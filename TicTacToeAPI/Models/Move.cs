using Microsoft.EntityFrameworkCore;

namespace TicTacToeAPI.Models
{
    [Index(nameof(RequestHash))]
    public class Move
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Game Game { get; set; } = null!;
        public Guid GameId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Column { get; set; }
        public char Symbol { get; set; }
        public bool RandomEvent { get; set; }
        public int MoveNumber { get; set; }
        public string RequestHash { get; set; } = string.Empty;
    }
}
