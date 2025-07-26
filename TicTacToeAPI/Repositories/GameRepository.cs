using Microsoft.EntityFrameworkCore;
using TicTacToeAPI.Data;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Models;

namespace TicTacToeAPI.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly TicTacToeContext context;
        private readonly ILogger<GameRepository> logger;
        public GameRepository(TicTacToeContext context, ILogger<GameRepository> logger) 
        {
            this.context = context;
            this.logger = logger;
        }
        public async Task AddGameAsync(Game game)
        {
            await context.AddAsync(game);
            await context.SaveChangesAsync();
        }
        public async Task<Game?> GetGameByIdAsync(Guid id)
        {
            Game? game = await context.Games.FindAsync(id);
            return game;
        }
        public async Task<Move?> FindMoveByHashAsync(string requestHash)
        {
            Move? move = await context.Moves.Include(m => m.Game).Where(m => m.RequestHash == requestHash).FirstOrDefaultAsync();
            return move;
        }
        public async Task SaveGameAndMoveAsync(Game game, Move move)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                context.Games.Update(game);
                await context.Moves.AddAsync(move);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Ошибка при сохранении игры и хода из-за конфликта данных");
                await transaction.RollbackAsync();
                throw; 
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "Ошибка при сохранении игры и хода");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
