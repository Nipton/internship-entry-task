using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Models.DTO;

namespace TicTacToeAPI.Controllers
{
    [Route("moves")]
    [ApiController]
    public class MovesController : ControllerBase
    {
        private readonly IGameService gameService;
        private readonly ILogger<MovesController> logger;
        public MovesController(IGameService gameService, ILogger<MovesController> logger)
        {
            this.gameService = gameService;
            this.logger = logger;
        }
        /// <summary>
        /// Выполняет ход в указанной игре.
        /// </summary>
        /// <param name="moveRequest">Информация о ходе (ID игры, игрок, координаты).</param>
        /// <param name="clientETag">ETag, соответствующий текущей версии игры. Передаётся в заголовке If-Match.</param>
        /// <returns>Ответ с результатом хода и обновлённым ETag в заголовке.</returns>
        /// <response code="200">Ход успешно выполнен.</response>
        /// <response code="400">Ошибка валидации параметров (например, ячейка уже занята, неверный игрок, некорректный формат запроса, или игра завершена).</response>
        /// <response code="404">Игра с указанным ID не найдена.</response>
        /// <response code="409">Конфликт версий: данные игры были изменены между запросами. Повторите попытку с актуальной версией после обновления игры.</response>
        /// <response code="500">Внутренняя ошибка сервера.</response>
        [HttpPost]
        public async Task<ActionResult<MoveResponse>> MakeMove([FromBody] MoveRequest moveRequest, [FromHeader(Name = "If-Match")] string clientETag)
        {
            logger.LogInformation("Получен запрос на ход: GameId={GameId}, Player={Player}, Row={Row}, Column={Column}",
                moveRequest.GameId, moveRequest.PlayerName, moveRequest.Row, moveRequest.Column);
            moveRequest.ETag = clientETag;
            var response = await gameService.MakeMoveAsync(moveRequest);
            Response.Headers.ETag = response.ETag;
            return Ok(response);
        }
    }
}
