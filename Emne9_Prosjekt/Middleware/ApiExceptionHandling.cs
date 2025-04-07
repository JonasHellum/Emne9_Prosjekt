using System.Data;
using System.Net;
using System.Text.Json;

namespace Emne9_Prosjekt.Middleware;

public class ApiExceptionHandling
{
    private readonly ILogger<ApiExceptionHandling> _logger;
    private readonly RequestDelegate _next;

    public ApiExceptionHandling(ILogger<ApiExceptionHandling> logger, RequestDelegate next)
    {
        _logger = logger;
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogWarning($"Unauthorized access: {e}");
            await HandleUnauthorizedExceptionAsync(context, e);
        }
        catch (KeyNotFoundException e)
        {
            _logger.LogWarning($"Key not found: {e}");
            await HandleKeyNotFoundExceptionAsync(context, e);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogWarning($"Invalid operation: {e}");
            await HandleInvalidOperationExceptionAsync(context, e);
        }
        catch (DataException e)
        {
            _logger.LogWarning($"Data exception: {e}");
            await HandleDataExceptionAsync(context, e);
        }
        catch (Exception e)
        {
            _logger.LogError($"Something went wring: {e}");
            await HandleExceptionAsync(context, e);
        }
    }
    
    public static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Internal Server Error. Please try again later.",
            Detailed = exception.Message
        };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
    
    private static Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Unauthorized access.",
            Detailed = exception.Message
        };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
    
    private static Task HandleKeyNotFoundExceptionAsync(HttpContext context, KeyNotFoundException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
    
        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "The requested resource was not found.",
            Detailed = exception.Message
        };
    
        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
    
    private static Task HandleInvalidOperationExceptionAsync(HttpContext context, InvalidOperationException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    
        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "The request was invalid.",
            Detailed = exception.Message
        };
        
        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
    
    private static Task HandleDataExceptionAsync(HttpContext context, DataException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        
        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Data transfer failed.",
            Detailed = exception.Message
        };
        
        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
    
}