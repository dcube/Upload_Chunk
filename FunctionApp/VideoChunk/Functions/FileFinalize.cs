using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VideoChunk.Domain;
using VideoChunk.Services;

namespace VideoChunk.Functions
{
    public class FileFinalize
    {
        private readonly IOptions<ApplicationSettings> _options;

        public FileFinalize(IOptions<ApplicationSettings> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName("FileFinalize")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/finalize")] HttpRequest req,
            ILogger log,
            CancellationToken ct)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            
            try
            {
                Guid fileId = new Guid(req.Query["fileId"].ToString());
                string fileName = req.Query["filename"].ToString();

                string jsonMessage = JsonConvert.SerializeObject(new FileMessage()
                {
                    FileId = fileId,
                    FileName = fileName
                });
                ServiceBusClient client = new ServiceBusClient(_options.Value.ServicebusConnectionString);
                ServiceBusSender sender = client.CreateSender(Constants.QueueName);
                await sender.SendMessageAsync(new ServiceBusMessage(jsonMessage), ct);

                return new OkObjectResult(new { IsSuccess = true });
            }
            catch (Exception e)
            {
                log.LogError(e, "Erreur dans la fonction FileChunk");
                return new OkObjectResult(new { IsSuccess = false });
            }
        }
    }
}
