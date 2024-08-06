using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
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
                var ignoredLabels = moderationConfig.IgnoredLabels;
                if (ignoredLabels.Any())
                {
                    var detectedLabels = await _rekognitionClient.DetectLabelsAsync(new DetectLabelsRequest
                    {
                        Settings = new DetectLabelsSettings
                        {
                            GeneralLabels = new GeneralLabelsSettings
                            {
                                LabelCategoryExclusionFilters = null,
                                LabelCategoryInclusionFilters = null,
                                LabelExclusionFilters = null,
                                LabelInclusionFilters = ignoredLabels
                            },
                            ImageProperties = null
                        },
                        Image = new Image
                        {
                            S3Object = new Amazon.Rekognition.Model.S3Object
                            {
                                Bucket = record.S3.Bucket.Name,
                                Name = record.S3.Object.Key
                            }
                        }
                    });

                    if (detectedLabels.Labels.Any(q => ignoredLabels.Contains(q.Name)))
                    {
                        await PublishProblematicImage(moderationConfig, record.S3, new List<Tag>());
                        return;
                    }
                }

                var detectModerationLabelsResponse =
                    await _rekognitionClient.DetectModerationLabelsAsync(detectModerationLabelsRequest);

                var objectTagging = new Tagging();
                objectTagging.TagSet.AddRange(tag.Tagging);

                foreach (var label in detectModerationLabelsResponse.ModerationLabels)
                {
                    var tagKey = GenerateSlug(label.Name);

                    if (tagKey.Length > 128)
                        tagKey = tagKey[..128];

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
                
                await PublishProblematicImage(moderationConfig, record.S3, objectTagging.TagSet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            
        }
        
    }

    private async Task<bool> PublishProblematicImage(ImageModerationConfig moderationConfig, S3Event.S3Entity record, List<Tag> tag)
    {
        if (!string.IsNullOrEmpty(moderationConfig.TopicArn))
        {
            var publishedStatus = await _amazonSimpleNotificationService.PublishAsync(new PublishRequest
            {
                TopicArn = moderationConfig.TopicArn,
                Message = JsonSerializer.Serialize(new EventModel<object>("ImageModeration", new
                {
                    Key = record.Object.Key,
                    Bucket = record.Bucket.Name,
                    Tags = tag.Select(q => new { Key = q.Key, Value = q.Value })
                        .ToDictionary(q => q.Key, q => q.Value)
                }))
            });

            if (publishedStatus.HttpStatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Message published successfully");
            }
        }
        return true;
    }

    private static string GenerateSlug(string phrase)
    {
        var str = RemoveAccent(phrase).ToLower();
        // invalid chars           
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // convert multiple spaces into one space   
        str = Regex.Replace(str, @"\s+", " ").Trim();
        // cut and trim 
        str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
        str = Regex.Replace(str, @"\s", "-"); // hyphens   
        return str;
    }

    private static string RemoveAccent(string txt)
    {
        var bytes = System.Text.Encoding.GetEncoding("UTF-8").GetBytes(txt);
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
}