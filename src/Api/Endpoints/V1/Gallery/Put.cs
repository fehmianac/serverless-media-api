using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery;

public class Put : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string itemId,
        [FromBody] GalleryPutRequest request,
        [FromServices] IApiContext apiContext,
        [FromServices] IGalleryRepository galleryRepository,
        [FromServices] IFileService fileService,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var gallery = await galleryRepository.GetGalleryAsync(apiContext.CurrentUserId, itemId, cancellationToken);
        if (gallery == null)
        {
            gallery = new GalleryEntity
            {
                Description = request.Description,
                Images = request.Images.Select(q => new GalleryEntity.GalleryImageModel
                {
                    Rank = request.Images.IndexOf(q),
                    Url = q.Url,
                    Id = Guid.NewGuid().ToString("N")
                }).ToList(),
                Name = request.Name,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
                ItemId = itemId,
                UserId = apiContext.CurrentUserId
            };
        }
        else
        {
            gallery.Description = request.Description;
            gallery.Name = request.Name;
            gallery.UpdatedAt = utcNow;

            var removedImages = gallery.Images.Where(q => request.Images.All(w => w.Url != q.Url)).ToList();
            foreach (var removedImage in removedImages)
            {
                gallery.Images.Remove(removedImage);
            }

            gallery.Images.AddRange(request.Images.Where(q => gallery.Images.All(w => q.Url != w.Url)).Select(q => new GalleryEntity.GalleryImageModel
            {
                Rank = request.Images.IndexOf(q),
                Url = q.Url,
                Id = Guid.NewGuid().ToString("N")
            }));

            await fileService.MoveFileToDeletedFolderAsync(removedImages.Select(q => q.Url).ToList(), cancellationToken);
        }

        await galleryRepository.SaveGalleryAsync(gallery, cancellationToken);

        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("v1/galleries/{itemId}", Handler)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("Gallery");
    }
}

public class GalleryPutRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<GalleryImageRequest> Images { get; set; } = new();

    public class GalleryImageRequest
    {
        public string Url { get; set; } = default!;
        public int Rank { get; set; }
    }
}