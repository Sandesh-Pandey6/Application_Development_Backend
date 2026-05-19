using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Autopartspro.API.Filters;

/// <summary>
/// Turns business-rule exceptions into 400/401/404 JSON so the React app can show { message }.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusCode, message) = context.Exception switch
        {
            ArgumentException ex => (StatusCodes.Status400BadRequest, ex.Message),
            InvalidOperationException ex => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            _ => (0, null)
        };

        if (statusCode == 0)
            return;

        context.Result = new ObjectResult(new { message })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }
}
