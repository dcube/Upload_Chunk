using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VideoChunk.Domain;

namespace VideoChunk.Services
{
    public interface IFileChunkService
    {
        Task<Result<bool>> SaveChunkAsync(Guid id, int index, IFormFile file, CancellationToken ct);
        Task<Result<Uri>> MergeChunkAsync(FileMessage message, CancellationToken ct, string containerDestination = null);
    }
}
