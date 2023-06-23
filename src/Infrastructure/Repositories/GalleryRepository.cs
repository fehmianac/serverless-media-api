using Amazon.DynamoDBv2;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class GalleryRepository : DynamoRepository, IGalleryRepository
{
    public GalleryRepository(IAmazonDynamoDB dynamoDb) : base(dynamoDb)
    {
    }

    protected override string GetTableName() => "galleries";

    public async Task<GalleryEntity?> GetGalleryAsync(string userId, string itemId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GalleryEntity>($"galleries#{userId}", itemId, cancellationToken);
    }

    public async Task<bool> SaveGalleryAsync(GalleryEntity entity, CancellationToken cancellationToken = default)
    {
        return await SaveAsync(entity, cancellationToken);
    }

    public async Task<bool> DeleteGalleryAsync(string userId, string itemId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync($"galleries#{userId}", itemId, cancellationToken);
    }

    public async Task<(List<GalleryEntity>, string)> GetGalleryPagedAsync(string userId, int? limit, string? nextToken, CancellationToken cancellationToken)
    {
        var (galleries, token, _) = await GetPagedAsync<GalleryEntity>($"galleries#{userId}", nextToken, limit, cancellationToken);
        return (galleries, token);
    }
}