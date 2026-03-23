using System.Text.Json;
using EchoTrace.Domain.Exceptions;
using FluentValidation;

namespace EchoTrace.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
    {
        var (status, type, title, detail) = ex switch
        {
            ValidationException ve => (
                400,
                "https://echo-trace.com/errors/validation-error",
                "Validation failed",
                string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))),

            CycleDetectedException cde => (
                422,
                "https://echo-trace.com/errors/cycle-detected",
                "Circular dependency detected",
                cde.Message),

            DocumentIntegrityViolationException dive => (
                422,
                "https://echo-trace.com/errors/document-integrity-violation",
                "Document integrity check failed",
                dive.Message),

            UnauthorizedAccessException => (
                403,
                "https://echo-trace.com/errors/forbidden",
                "Access denied",
                "You do not have permission to perform this action."),

            KeyNotFoundException => (
                404,
                "https://echo-trace.com/errors/not-found",
                "Resource not found",
                ex.Message),

            _ => (
                500,
                "https://echo-trace.com/errors/internal-server-error",
                "An unexpected error occurred",
                "Please try again later.")
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type,
            title,
            status,
            detail,
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
