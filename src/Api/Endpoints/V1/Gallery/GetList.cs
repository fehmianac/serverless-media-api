using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery;

public class GetList : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromQuery] string itemIds,
        [FromServices] IGalleryRepository galleryRepository,
        CancellationToken cancellationToken)
    {
        var input = new Dictionary<string, string>();
        var ids = itemIds.Split(',');
        foreach (var id in ids)
        {
            var keyValue = id.Split(':');
            if (keyValue.Length != 2)
                continue;
            
            input.TryAdd(keyValue[0], keyValue[1]);
        }

        var galleryList = await galleryRepository.GetBatchGalleryAsync(input, cancellationToken);
        return Results.Ok(galleryList.Select(q => q.ToDto()).ToList());
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/galleries", Handler)
            .Produces<List<GalleryDto>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("Gallery");
    }
}