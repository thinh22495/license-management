using FluentValidation;
using LicenseManagement.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LicenseManagement.Api.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ValidationException validationException:
                var errors = validationException.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
                context.Result = new BadRequestObjectResult(ApiResponse.Fail(string.Join("; ", errors)));
                context.ExceptionHandled = true;
                break;

            case UnauthorizedAccessException:
                context.Result = new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized"));
                context.ExceptionHandled = true;
                break;

            case KeyNotFoundException:
                context.Result = new NotFoundObjectResult(ApiResponse.Fail("Resource not found"));
                context.ExceptionHandled = true;
                break;

            default:
                _logger.LogError(context.Exception, "Unhandled exception");
                context.Result = new ObjectResult(ApiResponse.Fail("Internal server error"))
                {
                    StatusCode = 500
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
