using Domain.Dto.Event;
using Domain.Entities;
using Domain.Entities.Base;
using Domain.Repositories;
using Domain.Services;

namespace Infrastructure.Services;

public class GalleryService : IGalleryService
{
    private readonly IGalleryRepository _galleryRepository;
    private readonly IFileService _fileService;
    private readonly IEventBusManager _eventBusManager;

    public GalleryService(IGalleryRepository galleryRepository, IFileService fileService, IEventBusManager eventBusManager)
    {
        _galleryRepository = galleryRepository;
        _fileService = fileService;
        _eventBusManager = eventBusManager;
    }

    public async Task<bool> SaveGallery(string itemId, GalleryEntity entity, CancellationToken cancellationToken)
    {
        var gallery = await _galleryRepository.GetGalleryAsync(entity.UserId, itemId, cancellationToken);
        var oldImages = new List<string>();
        var utcNow = DateTime.UtcNow;
        if (gallery == null)
        {
            entity.CreatedAt = utcNow;
            gallery = entity;
        }
        else
        {
            gallery.Description = entity.Description;
            gallery.Name = entity.Name;
            gallery.UpdatedAt = utcNow;
            oldImages.AddRange(gallery.Images.Select(q => q.Url).ToList());
            var removedImages = gallery.Images.Where(q => entity.Images.All(w => w.Url != q.Url)).ToList();
            foreach (var removedImage in removedImages)
            {
                gallery.Images.Remove(removedImage);
            }

            gallery.Images.AddRange(entity.Images.Where(q => gallery.Images.All(w => q.Url != w.Url)).Select(q =>
                new GalleryEntity.GalleryImageModel
                {
                    Rank = entity.Images.IndexOf(q),
                    Url = q.Url,
                    Id = Guid.NewGuid().ToString("N"),
                    Dimension = new GalleryEntity.GalleryImageModel.ImageDimension
                    {
                        Width = q.Dimension.Width,
                        Height = q.Dimension.Height
                    },
                }));

            await _fileService.MoveFileToDeletedFolderAsync(removedImages.Select(q => q.Url).ToList(),
                cancellationToken);
        }

        await _galleryRepository.SaveGalleryAsync(entity, cancellationToken);
        await ManageImageMapping(gallery, oldImages, cancellationToken);
        await CheckProblematicImages(entity, cancellationToken);
        return true;
    }

    public async Task<bool> ModerateImageAsync(ModerationPayload payload, CancellationToken cancellationToken)
    {
        var imageGalleryMapping  = await _galleryRepository.GetImageGalleryMappingAsync($"https://{payload.Bucket}/{payload.Key}", cancellationToken);
        if (imageGalleryMapping == null)
        {
            await _galleryRepository.SaveProblematicImageAsync(new ProblematicImagesEntity
            {
                ImageUrl = $"https://{payload.Bucket}/{payload.Key}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            return true;
        }

        var gallery = await _galleryRepository.GetGalleryAsync(imageGalleryMapping.UserId, imageGalleryMapping.ItemId,
            cancellationToken);
        if (gallery == null)
        {
            return true;
        }

        await CheckProblematicImages(gallery, cancellationToken);
        return true;
    }

    private async Task<bool> ManageImageMapping(GalleryEntity gallery, IReadOnlyCollection<string> oldImages,
        CancellationToken cancellationToken)
    {
        var deletableList = new List<IEntity>();
        var saveableList = new List<IEntity>();
        deletableList.AddRange(oldImages.Select(q => new ImageGalleryMappingEntity
        {
            ImageUrl = q
        }));
        saveableList.AddRange(gallery.Images.Select(q => new ImageGalleryMappingEntity
        {
            ImageUrl = q.Url,
            ItemId = gallery.ItemId,
            UserId = gallery.UserId,
            CreatedAt = DateTime.UtcNow
        }));
        await _galleryRepository.BatchWriteDataAsync(deletableList, saveableList, cancellationToken);
        return true;
    }

    private async Task<bool> CheckProblematicImages(GalleryEntity gallery, CancellationToken cancellationToken)
    {
        var galleryImages = gallery.Images.ToList();
        var problematicImages = await _galleryRepository.GetProblematicImagesAsync(galleryImages.Select(q=> q.Url).ToList(),cancellationToken);
        var deletableList = new List<string>();
        foreach (var image in galleryImages)
        {
            if (problematicImages.All(q => q.ImageUrl != image.Url))
            {
                continue;
            }

            deletableList.Add(image.Url);
        }

        if (deletableList.Any())
        {
            await _fileService.MoveFileToDeletedFolderAsync(deletableList, cancellationToken);
            gallery.Images.RemoveAll(q => deletableList.Contains(q.Url));
            await _galleryRepository.SaveGalleryAsync(gallery, cancellationToken);
            await _eventBusManager.ProblematicImagesDetectedAsync(gallery.UserId,gallery.ItemId, cancellationToken);    
        }
        
        return true;
    }
    
    
}