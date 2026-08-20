using Warden.Application.Common;

namespace Warden.WebApi.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            var statusCode = ex switch
            {
                NotFoundAppException => StatusCodes.Status404NotFound,
                ConflictAppException => StatusCodes.Status409Conflict,
                ValidationAppException => StatusCodes.Status400BadRequest,
                UnauthorizedAppException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest,
            };

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
        }
    }
}
