using Amazon.S3;
using Amazon.S3.Model;
using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Options;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Endpoints.V1.File;

public class Post : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromBody] UploadUrlPostRequest request,
        [FromServices] IApiContext apiContext,
        [FromServices] IAmazonS3 amazonS3,
        [FromServices] IOptionsSnapshot<UploadSettings> uploadSettingsOptions,
        [FromServices] IValidator<UploadUrlPostRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var allowedContentTypes = uploadSettingsOptions.Value.AllowedContentTypes;
        if (!allowedContentTypes.ContainsKey(request.ContentType))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {"ContentType", new[] {$"Allowed content types are: {string.Join(", ", allowedContentTypes.Select(q => q.Key))}"}}
            });
        }


        foreach (var defaultTag in uploadSettingsOptions.Value.DefaultTags.Where(defaultTag => !request.Tags.ContainsKey(defaultTag.Key)))
        {
            request.Tags.Add(defaultTag.Key, defaultTag.Value);
        }


        var userId = "anonymous";
        if (apiContext.IsLogged)
        {
            request.Tags.TryAdd("UserId", apiContext.CurrentUserId);
            userId = apiContext.CurrentUserId;
        }

        var baseFolder = uploadSettingsOptions.Value.BaseFolder;
        if (request.IsTemp)
        {
            request.Tags.TryAdd("lifetime", "transient");
            baseFolder = "temp";
        }
        
        if (request.FileName.Contains('.'))
            request.FileName = request.FileName.Split(".").First();
        var tags = string.Join("&", request.Tags.Select(q => $"{q.Key}={q.Value}"));
        var pathOfObject = $"{baseFolder}/{userId}/{request.FileName}.{allowedContentTypes[request.ContentType]}";
        pathOfObject = pathOfObject.Replace("//", "/");
        var preSignedUrl = await amazonS3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(uploadSettingsOptions.Value.ExpireTime),
            BucketName = uploadSettingsOptions.Value.BucketName,
            Key = pathOfObject,
            ContentType = request.ContentType,
            Headers =
            {
                ["x-amz-acl"] = "public-read",
                ["x-amz-tagging"] = tags
            },
        });

        var finalUrl = $"https://{uploadSettingsOptions.Value.BucketName.TrimEnd('/')}/{pathOfObject.TrimStart('/')}";

        return Results.Ok(new UploadUrlPostResponse(finalUrl, preSignedUrl, new Dictionary<string, string>
        {
            {"x-amz-acl", "public-read"},
            {"x-amz-tagging", tags},
        }));
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("v1/file/upload-url", Handler)
            .Produces<UploadUrlPostResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Upload");
    }

    public class UploadUrlPostRequest
    {
        public string ContentType { get; set; } = default!;
        public string FileName { get; set; } = Guid.NewGuid().ToString();
        public Dictionary<string, string> Tags { get; set; } = new();
        public bool IsTemp { get; set; }

        public class UploadUrlPostRequestValidator : AbstractValidator<UploadUrlPostRequest>
        {
            public UploadUrlPostRequestValidator()
            {
            }
        }
    }

    public record UploadUrlPostResponse(string FinalObjectUrl, string SignedUrl, Dictionary<string, string> RequestHeaders);
}