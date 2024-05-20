using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Util;
using Api.Infrastructure;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Dto.Event;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.PubSub.Listener;

public class Post : IEndpoint
{
    private static async Task<IResult> Handler(
        HttpContext context,
        [FromServices] IAmazonSimpleNotificationService simpleNotificationService,
        [FromServices] IGalleryService galleryService,
        [FromServices] ILogger<EventListener> logger,
        CancellationToken cancellationToken)
    {
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        var message = Message.ParseMessage(body);


        Console.WriteLine(message.MessageText);
        var isValid = message.Validate();
        if (!isValid)
        {
            await simpleNotificationService.ConfirmSubscriptionAsync(message.TopicArn, message.Token,cancellationToken);
            return Results.BadRequest();
        }

        Console.WriteLine(message.MessageText);
        var eventModel = JsonSerializer.Deserialize<EventModel>(message.MessageText);
        
        if (eventModel?.EventName == null)
            return Results.Ok();
        
        var isProcessed = false;

        switch (eventModel.EventName)
        {
            case "ImageModeration":
                
                var imageModerationPayload = JsonSerializer.Deserialize<EventModel<ModerationPayload>>(message.MessageText);
                isProcessed = await galleryService.ModerateImageAsync(imageModerationPayload?.Data??new ModerationPayload(), cancellationToken);
                logger.LogInformation("Image moderation event processed. Event: {Event}", eventModel.EventName);
                Console.WriteLine("Image moderation event processed. Event: {Event}", eventModel.EventName);
                break;
            default:
                isProcessed = true;
                break;
            
        }

        if (!isProcessed)
        {
            logger.LogWarning("Failed to process event. Event: {Event}", eventModel.EventName);
            return Results.Problem(statusCode:(int)HttpStatusCode.BadGateway);
        }
            
        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    { 
        endpoints.MapPost("/v1/pub-sub/listener", Handler)
            .Produces((int)HttpStatusCode.OK)
            .WithTags("PubSub");
    }
}