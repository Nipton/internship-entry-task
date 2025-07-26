    using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Models.DTO;

namespace TicTacToeAPI.Controllers
{
    [Route("games")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly IGameService gameService;
        public GameController(IGameService gameService) 
        {
            this.gameService = gameService;
        }
        /// <summary>
        /// Создает новую игру.
        /// </summary>
        /// <param name="request">Данные для создания новой игры.</param>
        /// <returns>Ответ с данными созданной игры и ETag в заголовке.</returns>
        /// <response code="200">Игра успешно создана.</response>
        /// <response code="400">Некорректные параметры игрока. Возвращается, если переданы неверные имена или дубликаты игроков.</response>
        /// <response code="500">Внутренняя ошибка сервера.</response>
        [HttpPost]
        public  async Task<ActionResult<GameResponse>> CreateNewGame(CreateNewGameRequest request)
        {
            var response = await gameService.CreateGameAsync(request);
            Response.Headers.ETag = response.ETag;
            return Ok(response);
        }
        /// <summary>
        /// Возвращает информацию об игре по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор игры.</param>
        /// <returns>Данные игры, если найдена, или сообщение об ошибке.</returns>
        /// <response code="200">Игра найдена.</response>
        /// <response code="404">Игра с указанным ID не найдена.</response>
        /// <response code="500">Внутренняя ошибка сервера.</response>
        [HttpGet("id")]
        public async Task<ActionResult<GameResponse>> GetGameByIdAsync(Guid id)
        {
            var response = await gameService.GetGameAsync(id);
            if (response == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Игра не найдена",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"Игра с ID {id} не найдена.",
                    Instance = HttpContext.Request.Path
                };

                return NotFound(problemDetails);
            }
            Response.Headers.ETag = response.ETag;
            return Ok(response);
        }
    }
}
