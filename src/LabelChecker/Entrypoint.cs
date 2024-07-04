using System.Net;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using LabelChecker.Model;
using LabelChecker.Options;
using Tag = Amazon.S3.Model.Tag;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LabelChecker;

public class Entrypoint
{
    private readonly IAmazonRekognition _rekognitionClient;
    private readonly IAmazonS3 _amazonS3;
    private readonly IAmazonSimpleNotificationService _amazonSimpleNotificationService;
    private readonly IAmazonSimpleSystemsManagement _amazonSimpleSystemsManagement;

    public Entrypoint()
    {
        _rekognitionClient = new AmazonRekognitionClient();
        _amazonS3 = new AmazonS3Client();
        _amazonSimpleNotificationService = new AmazonSimpleNotificationServiceClient();
        _amazonSimpleSystemsManagement = new AmazonSimpleSystemsManagementClient();
    }

    public async Task Handler(S3Event @event, ILambdaContext context)
    {
        Console.WriteLine(JsonSerializer.Serialize(@event));
        var parameters = _amazonSimpleSystemsManagement.GetParameterAsync(new GetParameterRequest
        {
            Name = "/media-api/ImageModerationConfig",
        });

        ImageModerationConfig moderationConfig = new();
        if (parameters.Result.Parameter != null)
        {
            try
            {
                moderationConfig =
                    JsonSerializer.Deserialize<ImageModerationConfig>(parameters.Result.Parameter.Value) ?? new();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        if (!moderationConfig.IsEnabled)
            return;

        foreach (var record in @event.Records)
        {
            Console.WriteLine(JsonSerializer.Serialize(record));
            var detectModerationLabelsRequest = new DetectModerationLabelsRequest()
            {
                Image = new Image()
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object()
                    {
                        Name = record.S3.Object.Key,
                        Bucket = record.S3.Bucket.Name
                    },
                },
                MinConfidence = moderationConfig.MinConfidence
            };

            var tag = await _amazonS3.GetObjectTaggingAsync(new GetObjectTaggingRequest
            {
                Key = record.S3.Object.Key,
                BucketName = record.S3.Bucket.Name
            });

            try
            {
                var detectModerationLabelsResponse =
                    await _rekognitionClient.DetectModerationLabelsAsync(detectModerationLabelsRequest);

                var objectTagging = new Tagging();
                objectTagging.TagSet.AddRange(tag.Tagging);

                foreach (var label in detectModerationLabelsResponse.ModerationLabels)
                {
                    var tagKey = label.Name;

                    // Ensure the tag key complies with AWS constraints
                    if (!IsValidTagKey(tagKey))
                    {
                        // Modify the tag key to be valid
                        tagKey = SanitizeTagKey(tagKey);
                    }

                    objectTagging.TagSet.Add(new Tag
                    {
                        Key = tagKey,
                        Value = label.Confidence.ToString("F")
                    });
                    
                    
                }

                objectTagging.TagSet = objectTagging.TagSet.Take(10).ToList();
                Console.WriteLine($"Tagging: {JsonSerializer.Serialize(objectTagging)}");
                await _amazonS3.PutObjectTaggingAsync(new PutObjectTaggingRequest
                {
                    BucketName = record.S3.Bucket.Name,
                    ExpectedBucketOwner = null,
                    Key = record.S3.Object.Key,
                    RequestPayer = null,
                    Tagging = objectTagging,
                    VersionId = null,
                    ChecksumAlgorithm = null
                });

                Console.WriteLine(JsonSerializer.Serialize(detectModerationLabelsResponse.ModerationLabels));
                if (!detectModerationLabelsResponse.ModerationLabels.Any(q =>
                        moderationConfig.ForbiddenLabels.Any(x => q.Name.Contains(x)) &&
                        q.Confidence >= moderationConfig.AlertConfidence))
                {
                    Console.WriteLine("It's okay");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            if (!string.IsNullOrEmpty(moderationConfig.TopicArn))
            {
                var publishedStatus = await _amazonSimpleNotificationService.PublishAsync(new PublishRequest
                {
                    TopicArn = moderationConfig.TopicArn,
                    Message = JsonSerializer.Serialize(new EventModel<object>("ImageModeration", new
                    {
                        Key = record.S3.Object.Key,
                        Bucket = record.S3.Bucket.Name,
                        Tags = tag.Tagging.Select(q => new { Key = q.Key, Value = q.Value })
                            .ToDictionary(q => q.Key, q => q.Value)
                    }))
                });

                if (publishedStatus.HttpStatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("Message published successfully");
                }
            }
        }
    }

    // Function to check if a tag key is valid
    private static bool IsValidTagKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key.Length <= 128 && !key.StartsWith("aws:");
        // Add any other necessary checks for special characters if needed
    }

// Function to sanitize a tag key to make it valid
    private static string SanitizeTagKey(string key)
    {
        if (key.Length > 128)
        {
            key = key.Substring(0, 128);
        }

        if (key.StartsWith("aws:"))
        {
            key = key[4..]; // Remove the reserved prefix
        }

        // Replace or remove any invalid characters as needed
        return key;
    }
}