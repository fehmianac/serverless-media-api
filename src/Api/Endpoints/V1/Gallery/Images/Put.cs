using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery.Images;

public class Put : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string itemId,
        [FromBody] GalleryImagePutRequest request,
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

        if (string.IsNullOrEmpty(request.Id))
        {
            gallery.Images.Add(new GalleryEntity.GalleryImageModel
            {
                Id = Guid.NewGuid().ToString("N"),
                Rank = request.Rank,
                Url = request.Url,
                Dimension = new GalleryEntity.GalleryImageModel.ImageDimension
                {
                    Height = request.Dimension.Height,
                    Width = request.Dimension.Width
                }
            });
        }
        else
        {
            var galleryImage = gallery.Images.FirstOrDefault(x => x.Id == request.Id);
            if (galleryImage != null)
            {
                galleryImage.Rank = request.Rank;
                galleryImage.Url = request.Url;
                galleryImage.Dimension = new GalleryEntity.GalleryImageModel.ImageDimension
                {
                    Height = request.Dimension.Height,
                    Width = request.Dimension.Width
                };
            }
        }
        
        await galleryService.SaveGallery(itemId,gallery, cancellationToken);

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
    public string? Id { get; set; }
    public string Url { get; set; } = default!;
    public int Rank { get; set; }

    public GalleryImagePutRequestDimensionModel Dimension { get; set; } = default!;

    public class GalleryImagePutRequestDimensionModel
    {
        public int Height { get; set; }
        public int Width { get; set; }
    }
}