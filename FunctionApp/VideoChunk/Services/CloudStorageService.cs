using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;
using VideoChunk.Domain;

namespace VideoChunk.Services
{
    public class CloudStorageService: ICloudStorageService
    {
        private readonly IOptions<StorageSettings> _options;

        public CloudStorageService(IOptions<StorageSettings> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private BlobContainerClient GetBlobContainerClient(string containerName)
        {
            if (string.IsNullOrWhiteSpace(_options.Value.Key))
            {
                return new BlobContainerClient(this.GetContainerUri(containerName), new DefaultAzureCredential());
            }
            else
            {
                return new BlobContainerClient(this.GetContainerUri(containerName), new StorageSharedKeyCredential(_options.Value.Name, _options.Value.Key));
            }
        }

        private BlockBlobClient GetBlockBlobClient(string blobName, string containerName)
        {
            if (string.IsNullOrWhiteSpace(_options.Value.Key))
            {
                return new BlockBlobClient(this.GetBlobUri(blobName, containerName), new DefaultAzureCredential());
            }
            else
            {
                return new BlockBlobClient(this.GetBlobUri(blobName, containerName), new StorageSharedKeyCredential(_options.Value.Name, _options.Value.Key));
            }
        }

        private Uri GetContainerUri(string containerName)
        {
            return new Uri($"https://{_options.Value.Name}.blob.core.windows.net/{containerName}");
        }

        public Uri GetBlobUri(string blobName, string containerName)
        {
            return new Uri($"https://{_options.Value.Name}.blob.core.windows.net/{containerName}/{blobName}");
        }

        public async Task<string> UploadBlobAsync(Stream blobContent, string containerName, string blobName, CancellationToken ct)
        {
            if (containerName is null)
            {
                throw new ArgumentNullException(nameof(containerName));
            }

            if (blobName is null)
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            BlobContainerClient container = GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(cancellationToken:ct);
            BlobClient client = container.GetBlobClient(blobName);

            await client.UploadAsync(blobContent, true, ct);

            return client.Uri.ToString();
        }

        public IEnumerable<string> GetBlobList(string prefix, string containerName)
        {
            BlobContainerClient container = GetBlobContainerClient(containerName);
            return container.GetBlobs(prefix: prefix).Select(b => b.Name);
        }

        public async Task<Stream> GetBlobStreamAsync(string blobName, string containerName, CancellationToken ct)
        {
            BlobContainerClient container = GetBlobContainerClient(containerName);
            BlobClient client = container.GetBlobClient(blobName);
            return await client.OpenReadAsync(cancellationToken:ct);
        }

        public async Task<Stream> CreateBlobAsync(string blobName, string containerName, CancellationToken ct)
        {
            BlockBlobClient client = GetBlockBlobClient(blobName, containerName);
            return await client.OpenWriteAsync(true, cancellationToken:ct);
        }

        public async Task<bool> DeleteBlobAsync(string blobName, string containerName, CancellationToken ct)
        {
            BlobContainerClient container = GetBlobContainerClient(containerName);
            BlobClient client = container.GetBlobClient(blobName);
            var response = await client.DeleteIfExistsAsync(cancellationToken: ct);
            return response.Value;
        }
    }
}
