using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace TicTacToeAPI.Models.DTO
{
    public record MoveRequest
    {
        [JsonProperty(Order = 1)]
        public Guid GameId { get; set; }
        [Required(ErrorMessage = "Имя игрока обязательно.")]
        [MaxLength(50, ErrorMessage = "Имя игрока не может превышать 50 символов.")]
        [JsonProperty(Order = 2)]
        public string PlayerName { get; set; } = string.Empty;
        [JsonProperty(Order = 3)]
        public int Row { get; set; }
        [JsonProperty(Order = 4)]
        public int Column { get; set; }
        [JsonIgnore]
        public string ETag { get; set; } = string.Empty;
    }
}
