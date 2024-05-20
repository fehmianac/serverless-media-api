using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Repositories;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery.Images;

public class Delete : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string itemId,
        [FromRoute] string imageId,
        [FromServices] IApiContext apiContext,
        [FromServices] IGalleryRepository galleryRepository,
        [FromServices] IGalleryService galleryService,
        CancellationToken cancellationToken)
    {
        var userId = apiContext.CurrentUserId;
        var gallery = await galleryRepository.GetGalleryAsync(userId, itemId, cancellationToken);
        if (gallery == null)
        {
            return Results.NotFound();
        }

        var galleryImage = gallery.Images.FirstOrDefault(x => x.Id == imageId);
        if (galleryImage == null)
        {
            return Results.NotFound();
        }

        gallery.Images.Remove(galleryImage);
        
        await galleryService.SaveGallery(itemId, gallery, cancellationToken);
        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("v1/galleries/{itemId}/images/{imageId}", Handler)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("GalleryImages");
    }
}