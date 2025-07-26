using Microsoft.AspNetCore.Mvc;
using System.Net;
using TicTacToeAPI.Exceptions;

namespace TicTacToeAPI.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionHandlingMiddleware> logger;
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await next.Invoke(httpContext);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Произошло исключение.");
                await HandleExceptionAsync(httpContext, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Internal Server Error";
            var details = "Произошла ошибка при обработке запроса. Пожалуйста, попробуйте позже.";
            if (exception is PlayerValidationException)
            {
                message = "Переданы некорректные параметры.";
                details = exception.Message;
                statusCode = HttpStatusCode.BadRequest;
            }
            else if (exception is GameNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = "Переданы некорректные параметры.";
                details = exception.Message;
            }
            else if (exception is GameConflictException)
            {
                statusCode = HttpStatusCode.Conflict;
                message = "Конфликт версий.";
                details = exception.Message;
            }
            else if (exception is GameException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = "Ошибка игрового процесса.";
                details = exception.Message;
            }
                var problemDetails = new ProblemDetails
                {
                    Title = message,
                    Status = (int)statusCode,
                    Detail = details,
                    Instance = httpContext.Request.Path
                };
            HttpResponse response = httpContext.Response;
            response.ContentType = "application/json";
            response.StatusCode = (int)statusCode;
            await response.WriteAsJsonAsync(problemDetails);
        }
    }
}
