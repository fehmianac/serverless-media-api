using Amazon.DynamoDBv2;
using Domain.Entities;
using Domain.Entities.Base;
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

    public async Task<bool> BatchWriteDataAsync(List<IEntity> deleteEntities, List<IEntity> saveEntities, CancellationToken cancellationToken = default)
    {
         await base.BatchWriteAsync(saveEntities,deleteEntities, cancellationToken);
         return true;
    }

    public async Task<bool> DeleteImageGalleryMappingAsync(string url, CancellationToken cancellationToken)
    {
        return await base.DeleteAsync(ImageGalleryMappingEntity.GetPk(), url, cancellationToken);
    }

    public async Task<bool> SaveImageGalleryMappingAsync(string url, string itemId, string userId, CancellationToken cancellationToken)
    {
        var entity = new ImageGalleryMappingEntity
        {
            ImageUrl = url,
            ItemId = itemId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        return await base.SaveAsync(entity, cancellationToken);
    }

    public Task<ImageGalleryMappingEntity?> GetImageGalleryMappingAsync(string imageUrl, CancellationToken cancellationToken)
    {
        return GetAsync<ImageGalleryMappingEntity>(ImageGalleryMappingEntity.GetPk(), imageUrl, cancellationToken);
    }

    public Task<bool> SaveProblematicImageAsync(ProblematicImagesEntity problematicImagesEntity, CancellationToken cancellationToken)
    {
        return SaveAsync(problematicImagesEntity, cancellationToken);
    }

    public Task<List<ProblematicImagesEntity>> GetProblematicImagesAsync(List<string> toList, CancellationToken cancellationToken)
    {
        return BatchGetAsync(toList.Select(q => new ProblematicImagesEntity
        {
            ImageUrl = q
        }).ToList(), cancellationToken);
    }
}