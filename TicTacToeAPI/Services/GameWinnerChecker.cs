
using System.Diagnostics;
using TicTacToeAPI.Exceptions;
using TicTacToeAPI.Interfaces;

namespace TicTacToeAPI.Services
{
    public class GameWinnerChecker : IGameWinnerChecker
    {
        public bool CheckWinner(char[][] board, int winLineLength, int x, int y)
        {
            Debug.Assert(board != null, "Board не может быть null. Должна быть проверка в вызывающем коде.");
            Debug.Assert(board.Length >= 3, "Board должен быть инициализирован. Должна быть проверка в вызывающем коде.");
            Debug.Assert(winLineLength >= 3 && winLineLength <= board.Length, "Длина линии победы должна быть от 3 до размера поля включительно. Должна быть проверка в вызывающем коде.");
            Debug.Assert(x >= 0 && x < board.Length, $"X={x} выходит за границы доски. Должна быть проверка в вызывающем коде.");
            Debug.Assert(y >= 0 && y < board.Length, $"Y={y} выходит за границы доски. Должна быть проверка в вызывающем коде.");
            char symbol = board[x][y];
            if (symbol == ' ') return false;
            if (CheckHorizontal(board, winLineLength, x, y, symbol) || CheckVertical(board, winLineLength, x, y, symbol) || CheckDiagonal1(board, winLineLength, x, y, symbol) || CheckDiagonal2(board, winLineLength, x, y, symbol))
                return true;
            return false;
        }
        private int CountInDirection(char[][] board, int x, int y, int dx, int dy, char symbol)
        {
            int count = 0;
            int i = x + dx;
            int j = y + dy;
            while (IsInBounds(board, i, j) && board[i][j] == symbol)
            {
                count++;
                i += dx;
                j += dy;
            } 
            return count;
        }
        private bool IsInBounds(char[][] board, int i, int j)
        {
            return i >= 0 && i < board.Length && j >= 0 && j < board[i].Length;
        }
        private bool CheckHorizontal(char[][] board, int winLineLength, int x, int y, char symbol)
        {
            int count = 1 + CountInDirection(board, x, y, 0, 1, symbol) + CountInDirection(board, x, y, 0, -1, symbol);
            return count >= winLineLength;
        }
        private bool CheckVertical(char[][] board, int winLineLength, int x, int y, char symbol)
        {
            int count = 1 + CountInDirection(board, x, y, 1, 0, symbol) + CountInDirection(board, x, y, -1, 0, symbol);
            return count >= winLineLength;
        }
        private bool CheckDiagonal1(char[][] board, int winLineLength, int x, int y, char symbol)
        {
            int count = 1 + CountInDirection(board, x, y, -1, -1, symbol) + CountInDirection(board, x, y, 1, 1, symbol);
            return count >= winLineLength;
        }
        private bool CheckDiagonal2(char[][] board, int winLineLength, int x, int y, char symbol)
        {
            int count = 1 + CountInDirection(board, x, y, -1, 1, symbol) + CountInDirection(board, x, y, 1, -1, symbol);
            return count >= winLineLength;
        }
    }
}
