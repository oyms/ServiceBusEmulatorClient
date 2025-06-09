using Skaar.ServiceBusEmulatorClient.Exceptions;

namespace Skaar.ServiceBusEmulatorClient.Http.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (QueueNotFoundException e)
        {
            logger.LogError(e, "Queue is not defined.");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(e.Message);
        }        
        catch (MessageNotFoundException e)
        {
            logger.LogError(e, "Message is not found.");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(e.Message);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unknown error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(e.Message);
        }
    }
}