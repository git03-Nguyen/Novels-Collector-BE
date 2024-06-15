using Microsoft.AspNetCore.Diagnostics;
using NovelsCollector.Application.Exceptions;

namespace NovelsCollector.WebAPI.Services
{
    public class MyExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<MyExceptionHandler> _logger;

        public MyExceptionHandler(ILogger<MyExceptionHandler> logger) => _logger = logger;

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Log the exception
            _logger.LogError(exception, "An exception occurred while processing the request.");

            // Set the response status code
            httpContext.Response.StatusCode = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                BadHttpRequestException => StatusCodes.Status400BadRequest,
                BadRequestException => StatusCodes.Status400BadRequest,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            // Set the response payload
            var response = new
            {
                error = new
                {
                    message = exception.Message,
                    code = exception.HResult,
                }
            };

            // Write the response
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            // Indicate that the exception has been handled
            return true;
        }
    }
}
