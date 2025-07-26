using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TicTacToeAPI.Data;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Models;
using TicTacToeAPI.Models.DTO;
using TicTacToeAPI.Repositories;
using TicTacToeAPI.Services;

namespace TicTacToeAPI.Tests.IntegrationTests
{
    public class GameServiceIntegrationTests : IDisposable
    {
        private readonly Mock<IOptions<GameSettings>> settingsMock;
        private readonly IMapper mapper;
        private readonly Mock<ILogger<GameService>> loggerMock;
        private readonly Mock<ILogger<GameRepository>> loggerRepoMock;
        private readonly GameWinnerChecker winnerChecker;
        private readonly DbContextOptions<TicTacToeContext> dbOptions;
        private readonly TicTacToeContext context;
        private readonly GameService gameService;

        public GameServiceIntegrationTests()
        {
            settingsMock = new Mock<IOptions<GameSettings>>();
            loggerMock = new Mock<ILogger<GameService>>();
            loggerRepoMock = new Mock<ILogger<GameRepository>>();
            winnerChecker = new GameWinnerChecker();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            }, loggerFactory);
            mapper = configuration.CreateMapper();
            settingsMock.Setup(s => s.Value).Returns(new GameSettings { BoardSize = 3, WinLineLength = 3 });
            dbOptions = new DbContextOptionsBuilder<TicTacToeContext>().UseSqlite($"Data Source=test.db").Options;
            context = new TicTacToeContext(dbOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var repo = new GameRepository(context, loggerRepoMock.Object);
            gameService = new GameService(settingsMock.Object, mapper, repo, loggerMock.Object, winnerChecker);
        }
        public void Dispose()
        {
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [Fact]
        public async Task CreateGameAsync_ShouldCreateGame()
        {
            var createRequest = new CreateNewGameRequest() { PlayerX = "playerX", PlayerO = "playerO" };

            var response = await gameService.CreateGameAsync(createRequest);
            var savedGame = await context.Games.Where(x =>  x.PlayerX == response.PlayerX).FirstOrDefaultAsync();

            Assert.NotNull(response);
            Assert.NotNull(savedGame);
            Assert.Equal("playerX", savedGame.PlayerX);
            Assert.Equal(GameStatus.InProgress, savedGame.Status);
        }

        [Fact]
        public async Task GetGameByIdAsync_ShouldReturnCorrectResponse_WhenGameExists()
        {
            var createRequest = new CreateNewGameRequest() { PlayerX = "playerX", PlayerO = "playerO" };

            var responseAfterCreateGame = await gameService.CreateGameAsync(createRequest);
            var savedGame = await context.Games.Where(x => x.PlayerX == responseAfterCreateGame.PlayerX).FirstOrDefaultAsync();
            var response = await gameService.GetGameAsync(responseAfterCreateGame.Id);
            var expectedETag = HashGenerator.GenerateETag(savedGame.Version);

            Assert.NotNull(response);
            Assert.Equal(responseAfterCreateGame.Id, response.Id);
            Assert.Equal(expectedETag, response.ETag);
            Assert.Equal(createRequest.PlayerX, response.PlayerX);
            Assert.Equal(createRequest.PlayerO, response.PlayerO);
        }
        [Fact]
        public async Task MakeMoveAsync_ShouldDeclareWinner_WhenWinningMoveIsMade()
        {
            var createRequest = new CreateNewGameRequest() { PlayerX = "playerX", PlayerO = "playerO" }; 

            var responseAfterCreateGame = await gameService.CreateGameAsync(createRequest);
            var moveRequest = new MoveRequest() { GameId = responseAfterCreateGame.Id, PlayerName = "playerX", ETag = responseAfterCreateGame.ETag, Row = 0, Column = 0 };
            var responseAfterMove = await gameService.MakeMoveAsync(moveRequest);

            await MakeMoveAsync("playerO", 1, 0);
            await MakeMoveAsync("playerX", 2, 1);
            await MakeMoveAsync("playerO", 1, 1);
            await MakeMoveAsync("playerX", 0, 2);
            await MakeMoveAsync("playerO", 2, 0);
            await MakeMoveAsync("playerX", 0, 1);

            var game = await context.Games.Include(g => g.Moves).AsNoTracking().FirstOrDefaultAsync(x => x.Id == responseAfterCreateGame.Id);

            Assert.NotNull(game);
            Assert.Equal(7, game.MoveCount);
            Assert.Equal(GameStatus.X_Won, game.Status);
            Assert.Equal(7, game.Moves.Count);

            async Task MakeMoveAsync(string player, int row, int col)
            {
                var response = await gameService.MakeMoveAsync(new MoveRequest
                {
                    GameId = responseAfterCreateGame.Id,
                    PlayerName = player,
                    Row = row,
                    Column = col,
                    ETag = responseAfterMove.ETag
                });
                responseAfterMove = response; // Обновляем ETag
            }
        }
        [Fact]
        public async Task MakeMoveAsync_ShouldReturnSameResponse_WhenDuplicateRequestSent()
        {
            var createRequest = new CreateNewGameRequest() { PlayerX = "playerX", PlayerO = "playerO" };

            var responseAfterCreateGame = await gameService.CreateGameAsync(createRequest);
            var moveRequest = new MoveRequest() { GameId = responseAfterCreateGame.Id, PlayerName = "playerX", ETag = responseAfterCreateGame.ETag, Row = 0, Column = 0 };
            var responseAfterFirstMove = await gameService.MakeMoveAsync(moveRequest);
            var responseAfterSecondMove = await gameService.MakeMoveAsync(moveRequest);
            var game = await context.Games.Include(g => g.Moves).AsNoTracking().FirstOrDefaultAsync(x => x.Id == responseAfterCreateGame.Id);

            Assert.Equal(responseAfterFirstMove.Message, responseAfterSecondMove.Message);
            Assert.Equal(responseAfterFirstMove.ETag, responseAfterSecondMove.ETag);
            Assert.Equal(1, game.MoveCount);
            Assert.Single(game.Moves);
        }
    }

}
