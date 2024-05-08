using Domain.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure.Middleware;

public class ApiKeyValidatorMiddleware : IMiddleware
{
    private readonly IOptionsSnapshot<ApiKeyValidationSettings> _apiKeyValidationSettings;

    public ApiKeyValidatorMiddleware(IOptionsSnapshot<ApiKeyValidationSettings> apiKeyValidationSettings)
    {
        _apiKeyValidationSettings = apiKeyValidationSettings;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var whiteListPaths = _apiKeyValidationSettings.Value.WhiteList;
        whiteListPaths.Add("ping");
        if (!_apiKeyValidationSettings.Value.IsEnabled || whiteListPaths.Any(q => context.Request.Path.ToString().Contains(q)))
            return next(context);

        if (!context.Request.Headers.TryGetValue(_apiKeyValidationSettings.Value.HeaderName ?? "x-api-key", out var apikey) || apikey != _apiKeyValidationSettings.Value.ApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        return next(context);
    }
}