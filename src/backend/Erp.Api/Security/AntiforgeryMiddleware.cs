using Microsoft.AspNetCore.Antiforgery;

namespace Erp.Api.Security;

internal sealed class AntiforgeryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method)
            || !context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "A valid anti-forgery token is required.",
            });
            return;
        }

        await next(context);
    }
}
