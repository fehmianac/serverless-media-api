using Domain.Dto;
using Domain.Entities;
using Domain.Entities.Base;

namespace Domain.Repositories;

public interface IGalleryRepository
{
    Task<GalleryEntity?> GetGalleryAsync(string userId, string itemId, CancellationToken cancellationToken = default);
    Task<bool> SaveGalleryAsync(GalleryEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteGalleryAsync(string userId, string itemId, CancellationToken cancellationToken = default);
    Task<(List<GalleryEntity>, string)> GetGalleryPagedAsync(string userId, int? limit, string? nextToken, CancellationToken cancellationToken);
    Task<List<GalleryEntity>> GetBatchGalleryAsync(Dictionary<string,string> ids, CancellationToken cancellationToken);
    Task<bool> BatchWriteDataAsync(List<IEntity> deleteEntities, List<IEntity> saveEntities, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageGalleryMappingAsync(string url, CancellationToken cancellationToken);
    Task<bool> SaveImageGalleryMappingAsync(string url, string itemId, string userId, CancellationToken cancellationToken);
    Task<ImageGalleryMappingEntity?> GetImageGalleryMappingAsync(string imageUrl, CancellationToken cancellationToken);
    Task<bool> SaveProblematicImageAsync(ProblematicImagesEntity problematicImagesEntity, CancellationToken cancellationToken);
    Task<List<ProblematicImagesEntity>> GetProblematicImagesAsync(List<string> toList, CancellationToken cancellationToken);
}