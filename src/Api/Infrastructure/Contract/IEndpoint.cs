namespace Api.Infrastructure.Contract;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder endpoints);
}