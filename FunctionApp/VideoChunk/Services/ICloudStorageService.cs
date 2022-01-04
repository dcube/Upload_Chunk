using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VideoChunk.Services
{
    public interface ICloudStorageService
    {
        Task<string> UploadBlobAsync(Stream blobContent, string containerName, string blobName, CancellationToken ct);
        IEnumerable<string> GetBlobList(string prefix, string containerName);
        Task<Stream> GetBlobStreamAsync(string blobName, string containerName, CancellationToken ct);
        Task<Stream> CreateBlobAsync(string blobName, string containerName, CancellationToken ct);
        Task<bool> DeleteBlobAsync(string blobName, string containerName, CancellationToken ct);
        Uri GetBlobUri(string blobName, string containerName);
    }
}
