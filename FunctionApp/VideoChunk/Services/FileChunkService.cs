using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VideoChunk.Domain;

namespace VideoChunk.Services
{
    public class FileChunkService : IFileChunkService
    {
        private const string CHUNK_CONTAINER_NAME = "chunk";
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ILogger _logger;
        //private readonly ICloudStorageService _cloudStorageService;

        public FileChunkService(ICloudStorageService cloudStorageService, ILogger<FileChunkService> logger)
        {
            _cloudStorageService = cloudStorageService ?? throw new ArgumentNullException(nameof(cloudStorageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> SaveChunkAsync(Guid id, int index, IFormFile file, CancellationToken ct)
        {
            _logger.LogInformation($"FileChunkService.SaveChunk - index {index} pour le fichier {id}");
            if (index < 0)
            {
                string message = $"FileChunkService.SaveChunk - index {index} erroné pour le fichier {id}";
                _logger.LogError(message);
                return new Result<bool>()
                {
                    IsSuccess = false,
                    Message = message
                };
            }

            if (file == null)
            {
                string message = $"FileChunkService.SaveChunk - fichier vide pour le fichier {id}";
                _logger.LogError(message);
                return new Result<bool>()
                {
                    IsSuccess = false,
                    Message = message
                };
            }

            try
            {
                string blobName = $"{id}_{index}";
                await using Stream fileStream = file.OpenReadStream();
                await _cloudStorageService.UploadBlobAsync(fileStream, CHUNK_CONTAINER_NAME, blobName, ct);

                return new Result<bool>() { IsSuccess = true };
            }
            catch (Exception e)
            {
                string message = $"FileChunkService.SaveChunk - Erreur sur le fichier {id}, index {index} : {e.Message}";
                _logger.LogError(e, message);
                return new Result<bool>()
                {
                    IsSuccess = false,
                    Message = message
                };
            }
        }

        public async Task<Result<Uri>> MergeChunkAsync(FileMessage message, CancellationToken ct, string containerDestination = null)
        {
            if (message == null)
            {
                throw new ArgumentException("Le message désérialisé est vide");
            }
            if (string.IsNullOrWhiteSpace(message.FileName))
            {
                throw new ArgumentException("Le message ne contient pas le nom du fichier");
            }
            if (!message.FileId.HasValue)
            {
                throw new ArgumentException("Le message ne contient pas l'id du fichier");
            }
            try
            {
                var enumerable = _cloudStorageService.GetBlobList(message.FileId.ToString(), CHUNK_CONTAINER_NAME);
                if (enumerable == null)
                {
                    string errorMessage = $"Fichiers non trouvés pour l'id {message.FileId}";
                    _logger.LogError(errorMessage);
                    return new Result<Uri>()
                    {
                        IsSuccess = false,
                        Message = errorMessage
                    };
                }

                var list = enumerable.ToList();
                if (!list.Any())
                {
                    string errorMessage = $"Fichiers non trouvés pour l'id {message.FileId}";
                    _logger.LogError(errorMessage);
                    return new Result<Uri>()
                    {
                        IsSuccess = false,
                        Message = errorMessage
                    };
                }
                
                if (string.IsNullOrWhiteSpace(containerDestination))
                {
                    containerDestination = CHUNK_CONTAINER_NAME;
                }

                await using Stream streamWriter = await _cloudStorageService.CreateBlobAsync(message.FileName, containerDestination, ct);

                foreach (string file in list.OrderBy(f =>
                {
                    string[] split = f.Split('_');
                    int.TryParse(split[1], out int index);
                    return index;
                }))
                {
                    await using Stream reader = await _cloudStorageService.GetBlobStreamAsync(file, CHUNK_CONTAINER_NAME, ct);
                    reader.CopyTo(streamWriter);
                    _logger.LogInformation($"MergeChunkAsync - Merge du chunk {file}");
                }

                //Suppression des chunks après le merge
                foreach (string file in list)
                {
                    await _cloudStorageService.DeleteBlobAsync(file, CHUNK_CONTAINER_NAME, ct);
                }

                return new Result<Uri>()
                {
                    IsSuccess = true,
                    Value = _cloudStorageService.GetBlobUri(message.FileName, containerDestination)
                };
            }
            catch (Exception e)
            {
                string errorMessage = $"FileChunkService.MergeChunkAsync - Erreur sur le fichier {message.FileId} : {e.Message}";
                _logger.LogError(e, errorMessage);
                return new Result<Uri>()
                {
                    IsSuccess = false,
                    Message = errorMessage
                };
            }
        }
    }
}
