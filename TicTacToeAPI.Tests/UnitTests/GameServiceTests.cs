using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System.Xml.Linq;
using TicTacToeAPI.Exceptions;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Models;
using TicTacToeAPI.Models.DTO;
using TicTacToeAPI.Services;

namespace TicTacToeAPI.Tests.UnitTests
{
    public class GameServiceTests
    {
        private readonly Mock<IOptions<GameSettings>> settingsMock;
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<IGameRepository> gameRepoMock;
        private readonly Mock<ILogger<GameService>> loggerMock;
        private readonly Mock<IGameWinnerChecker> winnerCheckerMock;
        private readonly GameService gameService;

        public GameServiceTests()
        {
            settingsMock = new Mock<IOptions<GameSettings>>();
            mapperMock = new Mock<IMapper>();
            gameRepoMock = new Mock<IGameRepository>();
            loggerMock = new Mock<ILogger<GameService>>();
            winnerCheckerMock = new Mock<IGameWinnerChecker>();

            settingsMock.Setup(s => s.Value).Returns(new GameSettings { BoardSize = 3, WinLineLength = 3 });

            gameService = new GameService(
                settingsMock.Object,
                mapperMock.Object,
                gameRepoMock.Object,
                loggerMock.Object,
                winnerCheckerMock.Object
            );
        }
        [Fact]
        public async Task GetGameAsync_ExistingGame_ReturnsResponseWithETag()
        {
            // Arrange
            var game = new Game("playerX", "playerO", 3, 3, "[[' ', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, MoveCount = 0 };
            var expectedETag = HashGenerator.GenerateETag(game.Version);
            gameRepoMock.Setup(x => x.GetGameByIdAsync(game.Id)).ReturnsAsync(game);
            mapperMock.Setup(x => x.Map<GameResponse>(game)).Returns(new GameResponse() { Id = game.Id, Board = new char[0][] });

            // Act
            var result = await gameService.GetGameAsync(game.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedETag, result.ETag);
            Assert.Equal(game.Id, result.Id);
        }
        [Fact]
        public async Task GetGameAsync_NonExistentGame_ReturnsNull()
        {
            // Arrange
            var missingId = Guid.NewGuid();
            gameRepoMock.Setup(x => x.GetGameByIdAsync(missingId)).ReturnsAsync((Game?)null);

            // Act
            var result = await gameService.GetGameAsync(missingId);

            // Assert
            Assert.Null(result);
        }
        [Theory]
        #pragma warning disable xUnit1012 // Null should only be used for nullable parameters
        [InlineData(null, "Nagibator666", "Имя игрока обязательно и не может превышать 50 символов.")]
        [InlineData("Nagibator666", null, "Имя игрока обязательно и не может превышать 50 символов.")]
        #pragma warning restore xUnit1012 // Null should only be used for nullable parameters
        [InlineData("VeryLongNameVeryLongNameVeryLongNameVeryLongNameVeryLongName", "Nagibator666", "Имя игрока обязательно и не может превышать 50 символов.")]
        [InlineData("Nagibator666", "VeryLongNameVeryLongNameVeryLongNameVeryLongNameVeryLongName", "Имя игрока обязательно и не может превышать 50 символов.")]
        [InlineData("Nagibator666", "Nagibator666", "Имена не могут быть одинаковыми.")]
        public async Task CreateGameAsync_InvalidRequest_ThrowsPlayerValidationException(string playerX, string playerO, string errorMessage)
        {
            var request = new CreateNewGameRequest() { PlayerX = playerX, PlayerO = playerO };

            var exception = await Assert.ThrowsAsync<PlayerValidationException>(() => gameService.CreateGameAsync(request));
            Assert.Equal(errorMessage, exception.Message);
        }
        [Theory]
        [InlineData(-5, 5, "Некорректное значение размера поля. Значение должно быть целым числом от 3 до 100.")]
        [InlineData(5000, 10, "Некорректное значение размера поля. Значение должно быть целым числом от 3 до 100.")]
        [InlineData(3, 5, "Некорректное значение условия победы. Значение должно быть целым числом от 3 до размера поля включительно.")]
        [InlineData(3, -5, "Некорректное значение условия победы. Значение должно быть целым числом от 3 до размера поля включительно.")]
        public async Task CreateGameAsync_InvalidRequest_ThrowsGameValidationException(int boardSize, int winLineLength, string errorMessage)
        {
            Mock<IOptions<GameSettings>> invalidSettingsMock = new Mock<IOptions<GameSettings>>();
            invalidSettingsMock.Setup(s => s.Value).Returns(new GameSettings { BoardSize = boardSize, WinLineLength = winLineLength });
            var gameServiceWithinvalidSettings = new GameService(invalidSettingsMock.Object,mapperMock.Object,gameRepoMock.Object,loggerMock.Object,winnerCheckerMock.Object);

            var exception = await Assert.ThrowsAsync<GameValidationException>(() => gameServiceWithinvalidSettings.CreateGameAsync(new CreateNewGameRequest() { PlayerO = "O", PlayerX = "X"}));
            Assert.Equal(errorMessage, exception.Message);
        }
        [Fact]
        public async Task CreateGameAsync_ValidRequest_ReturnsGameResponse()
        {
            var game = new Game("playerX", "playerO", 3, 3, "[[' ', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, MoveCount = 0 };
            var request = new CreateNewGameRequest() { PlayerX = "playerX", PlayerO = "playerO" };
            var expectedETag = HashGenerator.GenerateETag(game.Version);
#pragma warning disable CS8600 // Преобразование литерала, допускающего значение NULL или возможного значения NULL в тип, не допускающий значение NULL.
            Game savedGame = null;
#pragma warning restore CS8600 // Преобразование литерала, допускающего значение NULL или возможного значения NULL в тип, не допускающий значение NULL.
            gameRepoMock.Setup(repo => repo.AddGameAsync(It.IsAny<Game>())).Callback<Game>(g => savedGame = g);
   
            mapperMock.Setup(m => m.Map<GameResponse>(It.Is<Game>(g => g == savedGame))).Returns(new GameResponse() { Board = new char[0][] });
            var response = await gameService.CreateGameAsync(request);
            
            
            Assert.NotNull(response);   
            Assert.NotNull(savedGame);
            var board = JsonConvert.DeserializeObject<char[][]>(savedGame.Board);
            Assert.NotNull(board);
            Assert.Equal(expectedETag, response.ETag);
            Assert.Equal(1, savedGame?.Version);
            Assert.Equal(request.PlayerX, savedGame?.CurrentPlayer);
            Assert.Equal(settingsMock.Object.Value.BoardSize, board?.Length);
            Assert.Equal(GameStatus.InProgress, savedGame?.Status);
            Assert.Equal(0, savedGame?.MoveCount);
            gameRepoMock.Verify(repo => repo.AddGameAsync(It.IsAny<Game>()), Times.Once);
        }
        [Fact]
        public async Task MakeMoveAsync_StandardMove_ReturnsValidResponse()
        {
            var game = new Game("playerX", "playerO", 3, 3, "[[' ', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, MoveCount = 0 };
            MoveRequest moveRequest = new MoveRequest { PlayerName = "playerX", Row = 0, Column = 1, ETag = HashGenerator.GenerateETag(game.Version), GameId = game.Id };
            var capturedMove = new Move();
            int oldGameVersion = game.Version;
            string expectedHash = HashGenerator.GenerateRequestHash(moveRequest);

            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(game.Id)).ReturnsAsync(game);
            gameRepoMock.Setup(r => r.SaveGameAndMoveAsync(It.IsAny<Game>(), It.IsAny<Move>())).Callback<Game, Move>((g, m) => { capturedMove = m; }).Returns(Task.CompletedTask);


            var response = await gameService.MakeMoveAsync(moveRequest);
            char[][]? newBoard = JsonConvert.DeserializeObject<char[][]>(game.Board);

            gameRepoMock.Verify(repo => repo.SaveGameAndMoveAsync(It.Is<Game>(g => g.Id == game.Id), It.Is<Move>(m => m.GameId == game.Id)), Times.Once);
            Assert.NotNull(response);
            Assert.NotNull(newBoard);
            Assert.NotNull(capturedMove);
            Assert.Equal('X', newBoard[0][1]);
            Assert.Equal(expectedHash, capturedMove.RequestHash);
            Assert.Equal(game.MoveCount, capturedMove.MoveNumber);
            Assert.Equal(game.Id, capturedMove.GameId);
            Assert.Equal(moveRequest.PlayerName, capturedMove.PlayerName);
            Assert.Equal('X', capturedMove.Symbol);
            Assert.Equal(oldGameVersion + 1, game.Version);
        }

        [Theory]
        [InlineData("")]
        #pragma warning disable xUnit1012 
        [InlineData(null)]
        #pragma warning restore xUnit1012 
        [InlineData("VeryLongNameVeryLongNameVeryLongNameVeryLongNameVeryLongName")]
        public async Task MakeMoveAsync_InvalidPlayerName_ThrowsPlayerValidationException(string name)
        {
            MoveRequest move = new MoveRequest { PlayerName = name };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);

            var exception = await Assert.ThrowsAsync<PlayerValidationException>(() => gameService.MakeMoveAsync(move));
            Assert.Contains("Имя игрока обязательно", exception.Message);
        }
        [Fact]
        public async Task MakeMoveAsync_GameNotFound_ThrowsGameNotFoundException()
        {
            MoveRequest move = new MoveRequest { PlayerName = "name", GameId = Guid.NewGuid() };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(move.GameId)).ReturnsAsync((Game)null!);

            var exception = await Assert.ThrowsAsync<GameNotFoundException>(() =>  gameService.MakeMoveAsync(move));
            Assert.Contains("Игра с идентификатором", exception.Message);
        }
        [Fact]
        public async Task MakeMoveAsync_ETagMismatch_ThrowsGameConflictException()
        {
            var game = new Game("playerX", "playerO", 3, 3, "[[' ', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, MoveCount = 0 };
            MoveRequest move = new MoveRequest { PlayerName = "playerX", ETag = "mismatchETag", GameId = game.Id };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(move.GameId)).ReturnsAsync(game);

            var exception = await Assert.ThrowsAsync<GameConflictException>(() => gameService.MakeMoveAsync(move));
            Assert.Equal("Версия игры устарела.", exception.Message);
        }
        [Theory]
        [InlineData(0, 50)]
        [InlineData(50, 0)]
        [InlineData(0, -50)]
        [InlineData(-50, 0)]
        public async Task MakeMoveAsync_InvalidCoordinates_ThrowsInvalidCoordinatesException(int row, int col)
        {
            var game = new Game("playerX", "playerO", 3, 3, "[[' ', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, BoardSize = 30, MoveCount = 0 };
            var currentETag = HashGenerator.GenerateETag(game.Version);
            MoveRequest move = new MoveRequest { PlayerName = "playerX", Column = col, Row = row, ETag = currentETag, GameId = game.Id };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(move.GameId)).ReturnsAsync(game);

            var exception = await Assert.ThrowsAsync<InvalidCoordinatesException>(() => gameService.MakeMoveAsync(move));
            Assert.Contains("Переданы некорректные значения координат.", exception.Message);
        }
        [Fact]
        public async Task MakeMoveAsync_WrongPlayerTurn_ThrowsWrongTurnException()
        {
            var game = new Game("playerX", "playerO", 3, 3, "[[' ', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, BoardSize = 3, MoveCount = 0, CurrentPlayer = "playerO" };
            var currentETag = HashGenerator.GenerateETag(game.Version);
            MoveRequest move = new MoveRequest { PlayerName = "playerX", Column = 0, Row = 0, ETag = currentETag, GameId = game.Id };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(move.GameId)).ReturnsAsync(game);

            var exception = await Assert.ThrowsAsync<WrongTurnException>(() => gameService.MakeMoveAsync(move));
            Assert.Contains("Сейчас не очередь игрока", exception.Message);
        }
        [Theory]
        [InlineData(GameStatus.Draw)]
        [InlineData(GameStatus.X_Won)]
        [InlineData(GameStatus.O_Won)]
        public async Task MakeMoveAsync_GameAlreadyFinished_ThrowsGameAlreadyFinishedException(GameStatus gameStatus)
        {
            var game = new Game("playerX", "playerO", 3, 3, "[[' ', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, BoardSize = 3, MoveCount = 0, CurrentPlayer = "playerX", Status = gameStatus};
            var currentETag = HashGenerator.GenerateETag(game.Version);
            MoveRequest move = new MoveRequest { PlayerName = "playerX", Column = 0, Row = 0, ETag = currentETag, GameId = game.Id };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(move.GameId)).ReturnsAsync(game);

            var exception = await Assert.ThrowsAsync<GameAlreadyFinishedException>(() => gameService.MakeMoveAsync(move));
            Assert.Equal("Игра уже завершена. Ход невозможен.", exception.Message);
        }

        [Fact]
        public async Task MakeMoveAsync_ExistingMove_ReturnsSavedResponse()
        {
            var game = new Game("playerX", "playerO", 3, 3, "[['X', ' ', ' '], [' ', 'O', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 3, BoardSize = 3, MoveCount = 2, CurrentPlayer = "playerX", };
            MoveRequest moveRequest = new MoveRequest { PlayerName = "playerX", Column = 2, Row = 2, GameId = game.Id };
            var hash = HashGenerator.GenerateRequestHash(moveRequest);
            Move existingMove = new Move() {GameId = game.Id, PlayerName = "playerX", Column = 2, Row = 2, RequestHash = hash, MoveNumber = 2};          
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(hash)).ReturnsAsync(existingMove);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(moveRequest.GameId)).ReturnsAsync(game);

            var result = await gameService.MakeMoveAsync(moveRequest);

            Assert.NotNull(result);
            Assert.Equal(HashGenerator.GenerateETag(game.Version), result.ETag);
            gameRepoMock.Verify(r => r.SaveGameAndMoveAsync(It.IsAny<Game>(), It.IsAny<Move>()), Times.Never);
        }
        [Fact]
        public async Task MakeMoveAsync_ExistingMoveWithChangedGame_ThrowsGameConflictException()
        {
            var game = new Game("playerX", "playerO", 3, 3, "[['X', ' ', ' '], [' ', 'O', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 3, BoardSize = 3, MoveCount = 3, CurrentPlayer = "playerX", };
            MoveRequest moveRequest = new MoveRequest { PlayerName = "playerX", Column = 2, Row = 2, GameId = game.Id };
            var hash = HashGenerator.GenerateRequestHash(moveRequest);
            Move existingMove = new Move() { GameId = game.Id, PlayerName = "playerX", Column = 2, Row = 2, RequestHash = hash, MoveNumber = 2 };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(hash)).ReturnsAsync(existingMove);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(moveRequest.GameId)).ReturnsAsync(game);

            var exception = await Assert.ThrowsAsync<GameConflictException>(() => gameService.MakeMoveAsync(moveRequest));
            Assert.Contains("Данные игры были изменены", exception.Message);
        }
        [Fact]
        public async Task MakeMoveAsync_CellAlreadyTaken_ThrowsCellAlreadyTakenException()
        {
            var game = new Game("playerX", "playerO", 3, 3, "[['X', ' ', ' '], [' ', ' ', ' '], [' ', ' ', ' ']]") { Id = Guid.NewGuid(), Version = 1, BoardSize = 3, CurrentPlayer = "playerX", };
            var currentETag = HashGenerator.GenerateETag(game.Version);
            var moveRequest = new MoveRequest { PlayerName = "playerX", Column = 0, Row = 0, GameId = game.Id, ETag = currentETag };
            

            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(moveRequest.GameId)).ReturnsAsync(game);

            var exception = await Assert.ThrowsAsync<CellAlreadyTakenException>(() => gameService.MakeMoveAsync(moveRequest));
            Assert.Equal($"Ячейка [{moveRequest.Row}, {moveRequest.Column}] уже занята.", exception.Message);
        }
        [Theory]
        [InlineData("playerX", "[[' ', ' ', ' '], [' ', 'X', 'O'], ['X', ' ', ' ']]", 3, GameStatus.X_Won)]
        [InlineData("playerO", "[[' ', ' ', ' '], [' ', 'X', 'O'], [' ', ' ', 'O']]", 3, GameStatus.O_Won)]
        [InlineData("playerX", "[['X', 'O', ' '], ['O', 'X', 'X'], ['O', 'X', 'O']]", 8, GameStatus.Draw)]
        [InlineData("playerX", "[[' ', 'O', ' '], [' ', ' ', ' '], ['X', ' ', ' ']]", 2, GameStatus.InProgress)]
        public async Task MakeMoveAsync_CheckGameStatusAndWinne_ReturnsValidResponse(string currentPlayer, string board, int moveCount, GameStatus gameStatus)
        {
            Game gameWithStatus = new Game("playerX", "playerO", 3, 3, board) { Id = Guid.NewGuid(), Version = 1, MoveCount = moveCount, CurrentPlayer = currentPlayer };
            MoveRequest moveRequest = new MoveRequest { PlayerName = currentPlayer, Row = 0, Column = 2, ETag = HashGenerator.GenerateETag(gameWithStatus.Version), GameId = gameWithStatus.Id };
            gameRepoMock.Setup(repo => repo.FindMoveByHashAsync(It.IsAny<string>())).ReturnsAsync((Move)null!);
            gameRepoMock.Setup(repo => repo.GetGameByIdAsync(gameWithStatus.Id)).ReturnsAsync(gameWithStatus);
            gameRepoMock.Setup(repo => repo.SaveGameAndMoveAsync(It.Is<Game>(g => g.Id == gameWithStatus.Id), It.IsAny<Move>())).Returns(Task.CompletedTask);
            GameWinnerChecker winnerChecker = new GameWinnerChecker();
            GameService gameService = new GameService(settingsMock.Object, mapperMock.Object, gameRepoMock.Object, loggerMock.Object, winnerChecker);

            var response = await gameService.MakeMoveAsync(moveRequest);

            Assert.NotNull(response);
            Assert.Equal(gameStatus, gameWithStatus.Status);
        }
    }
}
