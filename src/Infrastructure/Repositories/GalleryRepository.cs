using Amazon.DynamoDBv2;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class GalleryRepository : DynamoRepository, IGalleryRepository
{
    private readonly IEventBusManager _eventBusManager;

    public GalleryRepository(IAmazonDynamoDB dynamoDb, IEventBusManager eventBusManager) : base(dynamoDb)
    {
        _eventBusManager = eventBusManager;
    }

    protected override string GetTableName() => "galleries";

    public async Task<GalleryEntity?> GetGalleryAsync(string userId, string itemId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GalleryEntity>($"galleries#{userId}", itemId, cancellationToken);
    }

    public async Task<bool> SaveGalleryAsync(GalleryEntity entity, CancellationToken cancellationToken = default)
    {
        var response = await SaveAsync(entity, cancellationToken);
        await _eventBusManager.GalleryModifiedAsync(entity, cancellationToken);
        return response;
    }

    public async Task<bool> DeleteGalleryAsync(string userId, string itemId, CancellationToken cancellationToken = default)
    {
        var response = await DeleteAsync($"galleries#{userId}", itemId, cancellationToken);
        await _eventBusManager.GalleryDeletedAsync(userId, itemId, cancellationToken);
        return response;
    }

    public async Task<(List<GalleryEntity>, string)> GetGalleryPagedAsync(string userId, int? limit, string? nextToken, CancellationToken cancellationToken)
    {
        var (galleries, token, _) = await GetPagedAsync<GalleryEntity>($"galleries#{userId}", nextToken, limit, cancellationToken);
        return (galleries, token);
    }

    public async Task<List<GalleryEntity>> GetBatchGalleryAsync(Dictionary<string, string> ids, CancellationToken cancellationToken)
    {
        return await BatchGetAsync(ids.Select(q => new GalleryEntity
        {
            UserId = q.Key,
            ItemId = q.Value
        }).ToList(), cancellationToken);
    }
}