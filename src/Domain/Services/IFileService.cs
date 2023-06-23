namespace Domain.Services;

public interface IFileService
{
    Task<bool> MoveFileToDeletedFolderAsync(IList<string> urls, CancellationToken cancellationToken = default);
}