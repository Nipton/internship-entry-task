using TicTacToeAPI.Models.DTO;

namespace TicTacToeAPI.Interfaces
{
    public interface IGameService
    {
        Task<GameResponse> CreateGameAsync(CreateNewGameRequest request);
        Task<GameResponse?> GetGameAsync(Guid id);
        Task<MoveResponse> MakeMoveAsync(MoveRequest move);
    }
}
