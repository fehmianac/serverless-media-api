using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Entities;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery;

public class Put : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string itemId,
        [FromBody] GalleryPutRequest request,
        [FromServices] IApiContext apiContext,
        [FromServices] IGalleryService galleryService,
        [FromServices] IFileService fileService,
        CancellationToken cancellationToken)
    {
        
        var gallery = new GalleryEntity
        {
            Description = request.Description,
            Images = request.Images.Select(q => new GalleryEntity.GalleryImageModel
            {
                Rank = request.Images.IndexOf(q),
                Url = q.Url,
                Id = Guid.NewGuid().ToString("N"),
                Dimension = new GalleryEntity.GalleryImageModel.ImageDimension
                {
                    Width = q.Dimension.Width,
                    Height = q.Dimension.Height
                },
            }).ToList(),
            Name = request.Name,
            ItemId = itemId,
            UserId = apiContext.CurrentUserId
        };
        
        await galleryService.SaveGallery(itemId, gallery, cancellationToken);

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


        public DimensionModel Dimension { get; set; } = default!;

        public class DimensionModel
        {
            public int Height { get; set; }
            public int Width { get; set; }
        }
    }
}