using Amazon.S3;
using Amazon.S3.Model;
using Domain.Options;
using Domain.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class FileService : IFileService
{
    private readonly IAmazonS3 _amazonS3;
    private readonly IOptionsSnapshot<UploadSettings> _uploadSettingsOptions;

    public FileService(IAmazonS3 amazonS3, IOptionsSnapshot<UploadSettings> uploadSettingsOptions)
    {
        _amazonS3 = amazonS3;
        _uploadSettingsOptions = uploadSettingsOptions;
    }

    public async Task<bool> MoveFileToDeletedFolderAsync(IList<string> urls, CancellationToken cancellationToken = default)
    {
        var bucketName = _uploadSettingsOptions.Value.BucketName;
        var baseFolder = _uploadSettingsOptions.Value.BaseFolder;

        foreach (var url in urls)
        {
            var key = url.Replace(bucketName, string.Empty).Replace("https://", string.Empty).Replace("http://", string.Empty).TrimStart('/');
            await _amazonS3.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = bucketName,
                SourceKey = key,
                DestinationBucket = bucketName,
                DestinationKey = key.Replace(baseFolder, $"deleted"),
                CannedACL = S3CannedACL.Private
            }, cancellationToken);

            await _amazonS3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            }, cancellationToken);
        }

        return true;
    }
}