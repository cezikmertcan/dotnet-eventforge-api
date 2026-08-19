using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isDuplicate = exception is MongoWriteException mongoException &&
            mongoException.WriteError.Category == ServerErrorCategory.DuplicateKey;
        var statusCode = isDuplicate
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;
        var title = isDuplicate ? "A document with the same unique value already exists." : "An unexpected error occurred.";

        logger.LogError(exception, "Request {TraceId} failed with status {StatusCode}.", httpContext.TraceIdentifier, statusCode);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier
            }
        }, cancellationToken);

        return true;
    }
}
