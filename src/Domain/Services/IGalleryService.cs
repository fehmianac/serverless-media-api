using Domain.Dto.Event;
using Domain.Entities;

namespace Domain.Services;

public interface IGalleryService
{
    Task<bool> SaveGallery(string itemId, GalleryEntity entity, CancellationToken cancellationToken);
    Task<bool> ModerateImageAsync(ModerationPayload payload, CancellationToken cancellationToken);
}