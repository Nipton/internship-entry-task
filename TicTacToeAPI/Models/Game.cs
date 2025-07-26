using System.ComponentModel.DataAnnotations;

namespace TicTacToeAPI.Models
{
    public class Game
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [MaxLength(50)]
        public string PlayerX { get; set; }
        [MaxLength(50)]
        public string PlayerO { get; set; }
        public string CurrentPlayer { get; set; }
        public string Board { get; set; }
        public int BoardSize { get; set; }
        public int WinLineLength { get; set; }
        public int MoveCount { get; set; }
        public GameStatus Status { get; set; } = GameStatus.InProgress;
        public ICollection<Move> Moves { get; set; } = new List<Move>();
        [ConcurrencyCheck]
        public int Version { get; set; }
        public Game(string x, string o, int boardSize, int winLenght, string board) 
        {
            PlayerX = x;
            PlayerO = o;
            CurrentPlayer = x;
            Board = board;
            WinLineLength = winLenght;
            BoardSize = boardSize;
        }
        #pragma warning disable CS8618
        protected Game() { }
        #pragma warning restore CS8618 
    }
}
