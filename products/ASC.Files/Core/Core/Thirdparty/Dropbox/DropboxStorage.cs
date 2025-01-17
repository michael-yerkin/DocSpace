// (c) Copyright Ascensio System SIA 2010-2022
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ThumbnailSize = Dropbox.Api.Files.ThumbnailSize;

namespace ASC.Files.Thirdparty.Dropbox;

[Transient]
internal class DropboxStorage : IDisposable
{
    public bool IsOpened { get; private set; }
    public long MaxChunkedUploadFileSize = 20L * 1024L * 1024L * 1024L;

    private DropboxClient _dropboxClient;
    private readonly TempStream _tempStream;

    public DropboxStorage(TempStream tempStream)
    {
        _tempStream = tempStream;
    }

    public void Open(OAuth20Token token)
    {
        if (IsOpened)
        {
            return;
        }

        _dropboxClient = new DropboxClient(token.AccessToken);

        IsOpened = true;
    }

    public void Close()
    {
        _dropboxClient.Dispose();

        IsOpened = false;
    }



    public string MakeDropboxPath(string parentPath, string name)
    {
        return (parentPath ?? "") + "/" + (name ?? "");
    }

    public async Task<long> GetUsedSpaceAsync()
    {
        var spaceUsage = await _dropboxClient.Users.GetSpaceUsageAsync();

        return (long)spaceUsage.Used;
    }


    public Task<FolderMetadata> GetFolderAsync(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || folderPath == "/")
        {
            return Task.FromResult(new FolderMetadata(string.Empty, "/"));
        }

        return InternalGetFolderAsync(folderPath);
    }

    public async Task<FolderMetadata> InternalGetFolderAsync(string folderPath)
    {
        try
        {
            var metadata = await _dropboxClient.Files.GetMetadataAsync(folderPath);

            return metadata.AsFolder;
        }
        catch (AggregateException ex)
        {
            if (ex.InnerException is ApiException<GetMetadataError>
                && ex.InnerException.Message.StartsWith("path/not_found/"))
            {
                return null;
            }
            throw;
        }
    }

    public ValueTask<FileMetadata> GetFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || filePath == "/")
        {
            return ValueTask.FromResult<FileMetadata>(null);
        }

        return InternalGetFileAsync(filePath);
    }

    private async ValueTask<FileMetadata> InternalGetFileAsync(string filePath)
    {
        try
        {
            var data = await _dropboxClient.Files.GetMetadataAsync(filePath);

            return data.AsFile;
        }
        catch (AggregateException ex)
        {
            if (ex.InnerException is ApiException<GetMetadataError>
                && ex.InnerException.Message.StartsWith("path/not_found/"))
            {
                return null;
            }
            throw;
        }
    }


    public async Task<List<Metadata>> GetItemsAsync(string folderPath)
    {
        var data = await _dropboxClient.Files.ListFolderAsync(folderPath);
        return new List<Metadata>(data.Entries);
    }

    public async Task<Stream> GetThumbnailsAsync(string filePath, int width, int height)
    {
        try
        {
            var path = new PathOrLink.Path(filePath);
            var size = Convert(width, height);
            var arg = new ThumbnailV2Arg(path, size: size);

            var responce = await _dropboxClient.Files.GetThumbnailV2Async(arg);
            return await responce.GetContentAsStreamAsync();
        }
        catch
        {
            return null;
        }
    }

    private ThumbnailSize Convert(int width, int height)
    {
        if (width > 368)
        {
            return ThumbnailSize.W480h320.Instance;
        }
        else
        {
            return ThumbnailSize.W256h256.Instance;
        }
    }

    public Task<Stream> DownloadStreamAsync(string filePath, int offset = 0)
    {
        ArgumentNullOrEmptyException.ThrowIfNullOrEmpty(filePath);

        return InternalDownloadStreamAsync(filePath, offset);
    }

    public async Task<Stream> InternalDownloadStreamAsync(string filePath, int offset = 0)
    {
        using var response = await _dropboxClient.Files.DownloadAsync(filePath);
        var tempBuffer = _tempStream.Create();
        using (var str = await response.GetContentAsStreamAsync())
        {
            if (str != null)
            {
                await str.CopyToAsync(tempBuffer);
                await tempBuffer.FlushAsync();
                tempBuffer.Seek(offset, SeekOrigin.Begin);
            }
        }

        return tempBuffer;
    }

    public async Task<FolderMetadata> CreateFolderAsync(string title, string parentPath)
    {
        var path = MakeDropboxPath(parentPath, title);
        var result = await _dropboxClient.Files.CreateFolderV2Async(path, true);

        return result.Metadata;
    }

    public Task<FileMetadata> CreateFileAsync(Stream fileStream, string title, string parentPath)
    {
        var path = MakeDropboxPath(parentPath, title);

        return _dropboxClient.Files.UploadAsync(path, WriteMode.Add.Instance, true, body: fileStream);
    }

    public Task DeleteItemAsync(Metadata dropboxItem)
    {
        return _dropboxClient.Files.DeleteV2Async(dropboxItem.PathDisplay);
    }

    public async Task<FolderMetadata> MoveFolderAsync(string dropboxFolderPath, string dropboxFolderPathTo, string folderName)
    {
        var pathTo = MakeDropboxPath(dropboxFolderPathTo, folderName);
        var result = await _dropboxClient.Files.MoveV2Async(dropboxFolderPath, pathTo, autorename: true);

        return (FolderMetadata)result.Metadata;
    }

    public async Task<FileMetadata> MoveFileAsync(string dropboxFilePath, string dropboxFolderPathTo, string fileName)
    {
        var pathTo = MakeDropboxPath(dropboxFolderPathTo, fileName);
        var result = await _dropboxClient.Files.MoveV2Async(dropboxFilePath, pathTo, autorename: true);

        return (FileMetadata)result.Metadata;
    }

    public async Task<FolderMetadata> CopyFolderAsync(string dropboxFolderPath, string dropboxFolderPathTo, string folderName)
    {
        var pathTo = MakeDropboxPath(dropboxFolderPathTo, folderName);
        var result = await _dropboxClient.Files.CopyV2Async(dropboxFolderPath, pathTo, autorename: true);

        return (FolderMetadata)result.Metadata;
    }

    public async Task<FileMetadata> CopyFileAsync(string dropboxFilePath, string dropboxFolderPathTo, string fileName)
    {
        var pathTo = MakeDropboxPath(dropboxFolderPathTo, fileName);
        var result = await _dropboxClient.Files.CopyV2Async(dropboxFilePath, pathTo, autorename: true);

        return (FileMetadata)result.Metadata;
    }

    public async Task<FileMetadata> SaveStreamAsync(string filePath, Stream fileStream)
    {
        var metadata = await _dropboxClient.Files.UploadAsync(filePath, WriteMode.Overwrite.Instance, body: fileStream);

        return metadata.AsFile;
    }

    public async Task<string> CreateResumableSessionAsync()
    {
        var session = await _dropboxClient.Files.UploadSessionStartAsync(body: new MemoryStream());

        return session.SessionId;
    }

    public Task TransferAsync(string dropboxSession, long offset, Stream stream)
    {
        return _dropboxClient.Files.UploadSessionAppendV2Async(new UploadSessionCursor(dropboxSession, (ulong)offset), body: stream);
    }

    public Task<Metadata> FinishResumableSessionAsync(string dropboxSession, string dropboxFolderPath, string fileName, long offset)
    {
        var dropboxFilePath = MakeDropboxPath(dropboxFolderPath, fileName);
        return FinishResumableSessionAsync(dropboxSession, dropboxFilePath, offset);
    }

    public async Task<Metadata> FinishResumableSessionAsync(string dropboxSession, string dropboxFilePath, long offset)
    {
        return await _dropboxClient.Files.UploadSessionFinishAsync(
            new UploadSessionCursor(dropboxSession, (ulong)offset),
            new CommitInfo(dropboxFilePath, WriteMode.Overwrite.Instance));
    }

    public void Dispose()
    {
        if (_dropboxClient != null)
        {
            _dropboxClient.Dispose();
        }
    }
}
