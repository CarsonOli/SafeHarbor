using System.Net;
using System.Text.Json;
using SafeHarbor.DTOs;

namespace SafeHarbor.Infrastructure;

public sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Request failed with a not-found condition.");
            await WriteEnvelope(context, HttpStatusCode.NotFound, "not_found", "The requested resource was not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in request pipeline.");
            await WriteEnvelope(context, HttpStatusCode.InternalServerError, "server_error", "An unexpected error occurred.");
        }
    }

    private static Task WriteEnvelope(HttpContext context, HttpStatusCode statusCode, string code, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var envelope = new ApiErrorEnvelope(code, message, context.TraceIdentifier);
        return context.Response.WriteAsync(JsonSerializer.Serialize(envelope));
    }
}
