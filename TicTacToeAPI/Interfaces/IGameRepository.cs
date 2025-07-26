using TicTacToeAPI.Models;

namespace TicTacToeAPI.Interfaces
{
    public interface IGameRepository
    {
        Task AddGameAsync(Game game);
        Task<Game?> GetGameByIdAsync(Guid id);
        Task SaveGameAndMoveAsync(Game game, Move move);
        Task<Move?> FindMoveByHashAsync(string requestHash);
    }
}
