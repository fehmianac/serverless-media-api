using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Gallery;

public class GetPaged : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromQuery] int limit,
        [FromQuery] string? nextToken,
        [FromServices] IApiContext apiContext,
        [FromServices] IGalleryRepository galleryRepository,
        CancellationToken cancellationToken)
    {
        var (galleries, token) = await galleryRepository.GetGalleryPagedAsync(apiContext.CurrentUserId, limit, nextToken, cancellationToken);
        return Results.Ok(new PagedResponse<GalleryDto>
        {
            Data = galleries.Select(q => q.ToDto()).ToList(),
            Limit = limit,
            NextToken = token,
            PreviousToken = nextToken
        });
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/galleries", Handler)
            .Produces<PagedResponse<GalleryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("Gallery");
    }
}