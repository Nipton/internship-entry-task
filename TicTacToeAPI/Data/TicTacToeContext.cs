using Microsoft.EntityFrameworkCore;
using TicTacToeAPI.Models;

namespace TicTacToeAPI.Data
{
    public class TicTacToeContext : DbContext
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<Move> Moves { get; set; }

        public TicTacToeContext(DbContextOptions<TicTacToeContext> options) : base(options)
        {
        }
    }
}
