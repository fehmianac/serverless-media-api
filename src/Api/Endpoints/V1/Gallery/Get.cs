using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery;

public class Get : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string itemId,
        [FromServices] IApiContext apiContext,
        [FromServices] IGalleryRepository galleryRepository,
        CancellationToken cancellationToken)
    {
        var userId = apiContext.CurrentUserId;
        var gallery = await galleryRepository.GetGalleryAsync(userId, itemId, cancellationToken);
        return gallery == null ? Results.NotFound() : Results.Ok(gallery.ToDto());
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/galleries/{itemId}", Handler)
            .Produces<GalleryDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("Gallery");
    }
}