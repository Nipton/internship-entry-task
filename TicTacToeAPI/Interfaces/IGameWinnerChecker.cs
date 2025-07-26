namespace TicTacToeAPI.Interfaces
{
    public interface IGameWinnerChecker
    {
        bool CheckWinner(char[][] board, int winLineLength, int x, int y);
    }
}
