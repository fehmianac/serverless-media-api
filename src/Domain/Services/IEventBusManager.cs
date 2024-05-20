using Domain.Entities;

namespace Domain.Services;

public interface IEventBusManager
{
    Task<bool> GalleryModifiedAsync(GalleryEntity gallery, CancellationToken cancellationToken = default);
    Task<bool> GalleryDeletedAsync(string userId, string itemId, CancellationToken cancellationToken);
    Task<bool> ProblematicImagesDetectedAsync(string userId, string itemId, CancellationToken cancellationToken);
}