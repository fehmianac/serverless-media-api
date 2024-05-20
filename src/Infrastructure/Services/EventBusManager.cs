using System.Net;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Domain.Dto;
using Domain.Entities;
using Domain.Options;
using Domain.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class EventBusManager : IEventBusManager
{
    private readonly IAmazonSimpleNotificationService _amazonSimpleNotificationService;
    private readonly IOptionsSnapshot<EventBusSettings> _eventBusSettingsOptions;

    public EventBusManager(IAmazonSimpleNotificationService amazonSimpleNotificationService, IOptionsSnapshot<EventBusSettings> eventBusSettingsOptions)
    {
        _amazonSimpleNotificationService = amazonSimpleNotificationService;
        _eventBusSettingsOptions = eventBusSettingsOptions;
    }

    public async Task<bool> GalleryModifiedAsync(GalleryEntity gallery, CancellationToken cancellationToken = default)
    {
        return await PublishAsync(new EventModel<object>("GalleryModified", gallery), cancellationToken);
    }

    public async Task<bool> GalleryDeletedAsync(string userId, string itemId, CancellationToken cancellationToken)
    {
        return await PublishAsync(new EventModel<object>("GalleryModified", new {UserId = userId, ItemId = itemId}), cancellationToken);
    }

    public async Task<bool> ProblematicImagesDetectedAsync(string userId, string itemId, CancellationToken cancellationToken)
    {
        return await PublishAsync(new EventModel<object>("ProblematicImagesDetected", new {UserId = userId, ItemId = itemId}), cancellationToken);
    }

    private async Task<bool> PublishAsync(EventModel<object> eventModel, CancellationToken cancellationToken = default)
    {
        if (!_eventBusSettingsOptions.Value.IsEnabled)
            return true;

        var message = JsonSerializer.Serialize(eventModel);
        var snsResponse = await _amazonSimpleNotificationService.PublishAsync(_eventBusSettingsOptions.Value.TopicArn, message, cancellationToken);
        return snsResponse.HttpStatusCode is HttpStatusCode.OK or HttpStatusCode.Accepted or HttpStatusCode.Created;
    }
}