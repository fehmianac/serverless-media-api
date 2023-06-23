using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery.Images;

public class Put : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string itemId,
        [FromBody] GalleryImagePutRequest request,
        [FromServices] IApiContext apiContext,
        [FromServices] IGalleryRepository galleryRepository,
        CancellationToken cancellationToken)
    {
        var userId = apiContext.CurrentUserId;
        var gallery = await galleryRepository.GetGalleryAsync(userId, itemId, cancellationToken);
        if (gallery == null)
        {
            return Results.NotFound();
        }

        var galleryImage = gallery.Images.FirstOrDefault(x => x.Url == request.Url);
        if (galleryImage == null)
        {
            gallery.Images.Add(new GalleryEntity.GalleryImageModel
            {
                Id = Guid.NewGuid().ToString("N"),
                Rank = request.Rank,
                Url = request.Url
            });
        }
        else
        {
            galleryImage.Rank = request.Rank;
        }

        await galleryRepository.SaveGalleryAsync(gallery, cancellationToken);
        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("v1/galleries/{itemId}/images", Handler)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("GalleryImages");
    }
}

public class GalleryImagePutRequest
{
    public string Url { get; set; } = default!;
    public int Rank { get; set; }
}