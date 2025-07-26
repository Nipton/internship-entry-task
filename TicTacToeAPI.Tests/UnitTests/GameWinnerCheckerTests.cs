using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Services;

namespace TicTacToeAPI.Tests.UnitTests
{
    public class GameWinnerCheckerTests
    {
        public readonly IGameWinnerChecker winnerChecker = new GameWinnerChecker();
        private char[][] CreateEmptyBoard(int boardSize)
        {
            char[][] board = new char[boardSize][];
            for (int i = 0; i < boardSize; i++)
            {
                board[i] = new char[boardSize];
                for (int j = 0; j < boardSize; j++)
                {
                    board[i][j] = ' ';
                }
            }
            return board;
        }
        private void FillBoard(char[][] board, int startX, int startY, int dx, int dy, char symbol)
        {
            for (int i = 0; i < board.Length; i++)
            {
                int x = startX + i * dx;
                int y = startY + i * dy;
                if (x >= 0 && y >= 0 && x < board.Length && y < board.Length)
                    board[x][y] = symbol;
            }
        }

        [Theory]
        //Горизонтальные 
        [InlineData(5, 5, 0, 0, 0, 1, 0, 0)] // Проверка начала линии
        [InlineData(5, 5, 0, 0, 0, 1, 0, 2)] // Проверка середины линии
        [InlineData(5, 5, 0, 0, 0, 1, 0, 4)] // Проверка конца линии
        [InlineData(5, 3, 0, 2, 0, 1, 0, 3)] // Проверка c победной лиией меньше длины поля
        [InlineData(3, 3, 0, 0, 0, 1, 0, 2)] // Минимальное поле
        [InlineData(1000, 1000, 0, 0, 0, 1, 0, 0)] // Большое поле
        //Вертикальные
        [InlineData(5, 5, 0, 0, 1, 0, 0, 0)] // Проверка начала линии
        [InlineData(5, 5, 0, 0, 1, 0, 2, 0)] // Проверка середины линии
        [InlineData(5, 5, 0, 0, 1, 0, 4, 0)] // Проверка конца линии
        [InlineData(5, 4, 1, 0, 1, 0, 1, 0)] // Проверка c победной лиией меньше длины поля
        //Главная диагональ
        [InlineData(5, 5, 0, 0, 1, 1, 0, 0)] // Проверка начала линии
        [InlineData(5, 5, 0, 0, 1, 1, 2, 2)] // Проверка середины линии
        [InlineData(5, 5, 0, 0, 1, 1, 4, 4)] // Проверка конца линии
        [InlineData(5, 3, 1, 1, 1, 1, 1, 1)] // Проверка c победной лиией меньше длины поля
        [InlineData(5, 3, 1, 2, 1, 1, 2, 3)] // Проверка с диаганалью не по центру
        //Обратная диагональ
        [InlineData(5, 5, 0, 4, 1, -1, 0, 4)] // Проверка начала линии
        [InlineData(5, 5, 0, 4, 1, -1, 2, 2)] // Проверка середины линии
        [InlineData(5, 5, 0, 4, 1, -1, 4, 0)] // Проверка конца линии
        [InlineData(5, 3, 1, 3, 1, -1, 3, 1)] // Проверка c победной лиией меньше длины поля
        //Обратная диагональ
        public void CheckWinner_AllLines_ReturnsTrue(int size, int winLine, int startX, int startY, int dx, int dy, int checkX, int checkY)
        {
            var board = CreateEmptyBoard(size);
            FillBoard(board, startX, startY, dx, dy, 'X');

            bool result = winnerChecker.CheckWinner(board, winLine, checkX, checkY);

            Assert.True(result);
        }
        [Theory]
        [InlineData(5, 3, 0, 1, 0, 1, 0, 0)] // Точка вне линни, на границе
        [InlineData(5, 4, 0, 2, 0, 1, 0, 2)] // Не хватает 1 клетки до победы
        public void CheckWinner_NoWins_ReturnsFalse(int size, int winLine, int startX, int startY, int dx, int dy, int checkX, int checkY)
        {
            var board = CreateEmptyBoard(size);
            FillBoard(board, startX, startY, dx, dy, 'X');

            bool result = winnerChecker.CheckWinner(board, winLine, checkX, checkY);

            Assert.False(result);
        }
        [Theory]
        [InlineData(5, 5, 0, 0)] 
        public void CheckWinner_EmptySympol_ReturnsFalse(int size, int winLine, int checkX, int checkY)
        {
            var board = CreateEmptyBoard(size);

            bool result = winnerChecker.CheckWinner(board, winLine, checkX, checkY);

            Assert.False(result);
        }
        [Fact]
        public void CheckWinner_PartialBoard_ReturnsFalse()
        {
            var board = CreateEmptyBoard(5);
            
            board[0][0] = 'X';
            board[1][1] = 'O';
            board[2][2] = 'X';

            bool result = winnerChecker.CheckWinner(board, 3, 0, 0);

            Assert.False(result);
        }
        [Fact]
        public void CheckWinner_CrossingLines_DetectsCorrectWinner()
        {
            var board = CreateEmptyBoard(5);
            FillBoard(board, 2, 0, 0, 1, 'X');
            FillBoard(board, 0, 2, 1, 0, 'O');

            bool result = winnerChecker.CheckWinner(board, 3, 2, 2);

            Assert.True(result); 
        }
    }
}
