using System.ComponentModel.DataAnnotations;

namespace TicTacToeAPI.Models.DTO
{
    public class CreateNewGameRequest
    {
        [Required(ErrorMessage = "Имя игрока обязательно.")]
        [MaxLength(50, ErrorMessage = "Имя игрока не может превышать 50 символов.")]
        public string PlayerX { get; set; } = string.Empty;
        [Required(ErrorMessage = "Имя игрока обязательно.")]
        [MaxLength(50, ErrorMessage = "Имя игрока не может превышать 50 символов.")]
        public string PlayerO { get; set; } = string.Empty;
    }
}
