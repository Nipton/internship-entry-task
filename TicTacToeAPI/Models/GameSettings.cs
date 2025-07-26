namespace TicTacToeAPI.Models
{
    public class GameSettings
    {
        public int BoardSize { get; set; }
        public int WinLineLength { get; set; }
        public GameSettings() { }
        public GameSettings(int boardSize, int winLineLength)
        {
            BoardSize = boardSize;
            WinLineLength = winLineLength;
        }
    }
}
