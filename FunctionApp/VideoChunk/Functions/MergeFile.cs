using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using VideoChunk.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VideoChunk.Domain;

namespace VideoChunk.Functions
{
    public class MergeFile
    {
        private readonly IFileChunkService _fileChunkService;

        public MergeFile(IFileChunkService fileChunkService)
        {
            _fileChunkService = fileChunkService ?? throw new ArgumentNullException(nameof(fileChunkService));
        }

        [FunctionName("MergeFile")]
        public async Task Run(
            [ServiceBusTrigger(Constants.QueueName, Connection = "ServicebusConnectionString")]string queueItem,
            ILogger log,
            CancellationToken ct)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            if (string.IsNullOrWhiteSpace(queueItem))
            {
                log.LogError("MergeFile : Le message est vide");
                throw new ArgumentException("Le message est vide");
            }

            try
            {
                FileMessage message = JsonConvert.DeserializeObject<FileMessage>(queueItem);
                await _fileChunkService.MergeChunkAsync(message, ct);
            }
            catch (Exception e)
            {
                log.LogError(e, "Erreur dans la fonction MergeFile");
            }
        }
    }
}
