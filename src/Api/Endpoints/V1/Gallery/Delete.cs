using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Repositories;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery;

public class Delete : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string itemId,
        [FromServices] IApiContext apiContext,
        [FromServices] IGalleryRepository galleryRepository,
        [FromServices] IFileService fileService,
        CancellationToken cancellationToken)
    {
        var userId = apiContext.CurrentUserId;
        var gallery = await galleryRepository.GetGalleryAsync(userId, itemId, cancellationToken);
        if (gallery == null)
        {
            return Results.NotFound();
        }

        await galleryRepository.DeleteGalleryAsync(userId, itemId, cancellationToken);
        await fileService.MoveFileToDeletedFolderAsync(gallery.Images.Select(q => q.Url).ToList(), cancellationToken);
        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("v1/galleries/{itemId}", Handler)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("Gallery");
    }
}