using AutoMapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using TicTacToeAPI.Exceptions;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Models;
using TicTacToeAPI.Models.DTO;

namespace TicTacToeAPI.Services
{
    public class GameService : IGameService
    {
        private readonly IMapper mapper;
        private readonly GameSettings settings;
        private readonly IGameRepository gameRepo;
        private readonly ILogger<GameService> logger;
        private readonly IGameWinnerChecker winnerChecker;
        private static readonly Random rand = new();
        public GameService(IOptions<GameSettings> settings, IMapper mapper, IGameRepository gameRepository, ILogger<GameService> logger, IGameWinnerChecker winnerChecker)
        {
            this.settings = settings.Value;
            this.mapper = mapper;
            this.logger = logger;
            this.winnerChecker = winnerChecker;
            gameRepo = gameRepository;
        }
        private void ValidateGameCreation(CreateNewGameRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerX) || string.IsNullOrEmpty(request.PlayerO) || request.PlayerX.Length > 50 || request.PlayerO.Length > 50)
                throw new PlayerValidationException("Имя игрока обязательно и не может превышать 50 символов.");
            if (request.PlayerX.Equals(request.PlayerO))
                throw new PlayerValidationException("Имена не могут быть одинаковыми.");
            if (settings.BoardSize < 3 || settings.BoardSize > 100)
                throw new GameValidationException("Некорректное значение размера поля. Значение должно быть целым числом от 3 до 100.");
            if (settings.WinLineLength < 3 || settings.WinLineLength > settings.BoardSize)
                throw new GameValidationException("Некорректное значение условия победы. Значение должно быть целым числом от 3 до размера поля включительно.");
        }
        public async Task<GameResponse> CreateGameAsync(CreateNewGameRequest request)
        {
            ValidateGameCreation(request);
            int boardSize = settings.BoardSize;
            int winLineLength = settings.WinLineLength;
            char[][] board = new char[boardSize][];
            for (int i = 0; i < boardSize; i++)
            {
                board[i] = new char[boardSize];
                for (int j = 0; j < boardSize; j++)
                {
                    board[i][j] = ' ';
                }
            }
            string boardJson = JsonConvert.SerializeObject(board);
            Game newGame = new Game(request.PlayerX, request.PlayerO, boardSize, winLineLength, boardJson);
            newGame.Version = 1;
            await gameRepo.AddGameAsync(newGame);
            logger.LogInformation($"Создана новая игра с Id {newGame.Id}");
            var response = mapper.Map<GameResponse>(newGame);
            response.ETag = HashGenerator.GenerateETag(newGame.Version);
            return response;
        }
        public async Task<GameResponse?> GetGameAsync(Guid id)
        {
            Game? game = await gameRepo.GetGameByIdAsync(id);
            if (game == null)
            {
                logger.LogWarning("Игра с ID {GameId} не найдена.", id);
                return null;
            }
            logger.LogInformation("Игра с ID {GameId} успешно получена из базы данных.", id);
            var response = mapper.Map<GameResponse>(game);
            response.ETag = HashGenerator.GenerateETag(game.Version);
            return response;
        }
        public async Task<MoveResponse> MakeMoveAsync(MoveRequest moveRequest)
        {
            logger.LogInformation("Начало обработки хода: GameId={GameId}, Player={Player}, Row={Row}, Column={Column}", moveRequest.GameId, moveRequest.PlayerName, moveRequest.Row, moveRequest.Column);

            string requestHash = HashGenerator.GenerateRequestHash(moveRequest);
            Move? existingMove = await gameRepo.FindMoveByHashAsync(requestHash);
            if (existingMove != null)
            {
                logger.LogInformation("Повторный запрос, возвращается сохранённый результат.");
                var currentGame = await gameRepo.GetGameByIdAsync(existingMove.GameId);
                if (currentGame == null)
                    throw new GameNotFoundException(existingMove.GameId);
                if(existingMove.MoveNumber != currentGame.MoveCount)
                {
                    logger.LogWarning("Данные игры были изменены между запросами. Обновите игру.");
                    throw new GameConflictException("Данные игры были изменены, повторный запрос не может быть выполнен.");
                }
                var savedResponse = BuildResponse(currentGame, DeserializeBoard(currentGame.Board), existingMove);
                if (savedResponse != null)
                {
                    savedResponse.ETag = HashGenerator.GenerateETag(currentGame.Version);
                    return savedResponse;
                }
            }
            
            Game game = await ValidateMove(moveRequest);

            int x = moveRequest.Row;
            int y = moveRequest.Column;
            char[][] board = DeserializeBoard(game.Board); 
            if (!board[x][y].Equals(' '))
            {
                logger.LogWarning("Ячейка уже занята: GameId={GameId}, Row={Row}, Column={Column}", game.Id, x, y);
                throw new CellAlreadyTakenException(x, y);
            }

            char symbol = game.PlayerX == moveRequest.PlayerName ? 'X' : 'O';
            bool randomEvent = false;
            if ((game.MoveCount + 1) % 3 == 0)
            {
                if (rand.Next(1, 101) <= 10)
                {
                    symbol = game.PlayerX == moveRequest.PlayerName ? 'O' : 'X';
                    randomEvent = true;
                }
            }
            board[x][y] = symbol;
            game.Version++;
            UpdateGameAfterMove(game, board, x, y);
            logger.LogInformation("Обновление игры после хода: GameId={GameId}, MoveCount={MoveCount}, Status={Status}", game.Id, game.MoveCount, game.Status);

            Move move = ToMove(moveRequest, game, symbol, randomEvent, requestHash);
            await gameRepo.SaveGameAndMoveAsync(game, move);

            var response = BuildResponse(game, board, move);
            logger.LogInformation("Завершение обработки хода: GameId={GameId}, GameStatus={Status}, Winner={Winner}", game.Id, game.Status, response.Winner ?? "нет");
            return response;
        }
        private char[][] DeserializeBoard(string boardJson)
        {
            char[][] board = JsonConvert.DeserializeObject<char[][]>(boardJson) ?? throw new InvalidOperationException("Не удалось десериализовать игровое поле");
            if (board.Length < 3)
                throw new InvalidOperationException("Полученно некорректное поле после десериализации");
            return board;
        }
        private MoveResponse BuildResponse(Game game, char[][] board, Move move)
        {
            MoveResponse response = new MoveResponse();
            response.ETag = HashGenerator.GenerateETag(game.Version);
            response.Board = board;
            response.IsGameOver = !(game.Status is GameStatus.InProgress);
            response.Message = "";
            if (move.RandomEvent)
                response.Message = "Произошло случайное событие! ";
            response.Message += $"Игрок {move.PlayerName} ставит {move.Symbol} в клетку [{move.Row},{move.Column}]! ";
            if (game.Status == GameStatus.X_Won)
            {
                response.Winner = game.PlayerX;
                response.Message += $"Игрок {game.PlayerX} победил!";
            }
            else if (game.Status == GameStatus.O_Won)
            {
                response.Winner = game.PlayerO;
                response.Message += $"Игрок {game.PlayerO} победил!";
            }
            else if (game.Status == GameStatus.Draw)
            {
                response.Message += "Игра закончена. Ничья!";
            }
            return response;
        }
        private void UpdateGameAfterMove(Game game, char[][] board, int x, int y)
        {
            game.CurrentPlayer = game.CurrentPlayer == game.PlayerX ? game.PlayerO : game.PlayerX;
            game.MoveCount += 1;
            game.Board = JsonConvert.SerializeObject(board);
            if (game.MoveCount >= game.WinLineLength)
            {
                if (winnerChecker.CheckWinner(board, game.WinLineLength, x, y))
                {
                    if (board[x][y] == 'X')
                    {
                        game.Status = GameStatus.X_Won;
                    }
                    else
                    {
                        game.Status = GameStatus.O_Won;
                    }
                }
                else if (game.MoveCount == board.Length * board.Length)
                {
                    game.Status = GameStatus.Draw;
                }
            }
        }
        private Move ToMove(MoveRequest moveRequest, Game game, char symbol, bool randomEvent, string requestHash)
        {
            Move move = new Move()
            {
                PlayerName = moveRequest.PlayerName,
                Game = game,
                GameId = moveRequest.GameId,
                Row = moveRequest.Row,
                Column = moveRequest.Column,
                MoveNumber = game.MoveCount,
                Symbol = symbol,
                RequestHash = requestHash,
                RandomEvent = randomEvent,
            };
            return move;
        }
        private async Task<Game> ValidateMove(MoveRequest move)
        {
            logger.LogInformation("Начало валидации хода: GameId={GameId}, Player={Player}, Row={Row}, Column={Column}", move.GameId, move.PlayerName, move.Row, move.Column);
            if (string.IsNullOrEmpty(move.PlayerName) || move.PlayerName.Length > 50)
            {
                logger.LogWarning("Неверное имя игрока: '{PlayerName}'", move.PlayerName);
                throw new PlayerValidationException("Имя игрока обязательно и не может превышать 50 символов.");
            }
            Game? game = await gameRepo.GetGameByIdAsync(move.GameId);
            if (game == null)
            {
                throw new GameNotFoundException(move.GameId);
            }
            var currentETag = HashGenerator.GenerateETag(game.Version);
            if (move.ETag != currentETag)
            {
                throw new GameConflictException("Версия игры устарела.");
            }
            if (move.Row >= game.BoardSize || move.Row < 0 || move.Column >= game.BoardSize || move.Column < 0)
                throw new InvalidCoordinatesException(move.Row, move.Column);
            if (!game.CurrentPlayer.Equals(move.PlayerName))
                throw new WrongTurnException(move.PlayerName);
            if (!(game.Status == GameStatus.InProgress))
                throw new GameAlreadyFinishedException();
            return game;
        }
    }
}
