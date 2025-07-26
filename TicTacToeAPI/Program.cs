using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TicTacToeAPI.Data;
using TicTacToeAPI.Interfaces;
using TicTacToeAPI.Middleware;
using TicTacToeAPI.Models;
using TicTacToeAPI.Models.DTO;
using TicTacToeAPI.Repositories;
using TicTacToeAPI.Services;

namespace TicTacToeAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;

            builder.Services.Configure<GameSettings>(options =>
            {
                int.TryParse(config["GameSettings:BoardSize"], out int boardSize);
                int.TryParse(config["GameSettings:WinLineLength"], out int winLineLength);
                options.BoardSize = boardSize;
                options.WinLineLength = winLineLength;
            });
            builder.Services.AddDbContext<TicTacToeContext>(option => option.UseSqlite(config.GetConnectionString("DefaultConnection")));
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());           
            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();
            builder.Services.AddScoped<IGameWinnerChecker, GameWinnerChecker>();
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TicTacToeContext>();
                if (!dbContext.Database.GetAppliedMigrations().Any())
                    dbContext.Database.Migrate();
            }
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.MapGet("/health", () => Results.Ok("App is healthy."));
            app.MapControllers();
            app.Run();
        }
    }
}
