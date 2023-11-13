using System.Text;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Util;
using Api.Infrastructure;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.PubSub;

public class Post : IEndpoint
{
    private static async Task<IResult> Handler(
        HttpContext context,
        [FromServices] IAmazonSimpleNotificationService simpleNotificationService,
        [FromServices] IGalleryRepository galleryRepository,
        CancellationToken cancellationToken)
    {
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync();
        }

        var message = Message.ParseMessage(body);


        var isValid = message.Validate();
        if (!isValid)
        {
            await simpleNotificationService.ConfirmSubscriptionAsync(message.TopicArn, message.Token, cancellationToken);
            return Results.BadRequest();
        }

        var eventModel = JsonSerializer.Deserialize<EventModel>(message.MessageText);
        if (eventModel?.EventName is not "GalleryModified")
            return Results.Ok();

        var galleryModifiedEvent = JsonSerializer.Deserialize<EventModel<GalleryEntity>>(message.MessageText);
        if (galleryModifiedEvent?.Data == null)
            return Results.Ok();

        var missingDimensions = galleryModifiedEvent.Data.Images
            .Where(x => (x.Dimension == null) || (x.Dimension.Width == 0) || (x.Dimension.Height == 0))
            .ToList();

        if (!missingDimensions.Any())
        {
            return Results.Ok();
        }

        foreach (var missingDimension in missingDimensions)
        {
            try
            {
                var image = await Image.LoadAsync(missingDimension.Url, cancellationToken);
                missingDimension.Dimension = new GalleryEntity.GalleryImageModel.ImageDimension
                {
                    Width = image.Width,
                    Height = image.Height
                };
            }
            catch
            {
                missingDimension.Dimension = new GalleryEntity.GalleryImageModel.ImageDimension
                {
                    Width = -1,
                    Height = -1
                };
            }
        }

        await galleryRepository.SaveGalleryAsync(galleryModifiedEvent.Data, cancellationToken);
        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("v1/pub-sub/event-listener", Handler)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("PubSub");
    }
}