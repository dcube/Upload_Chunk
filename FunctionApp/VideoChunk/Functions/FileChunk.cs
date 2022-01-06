using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VideoChunk.Services;

namespace VideoChunk.Functions
{
    public class FileChunk
    {
        private readonly IFileChunkService _fileChunkService;

        public FileChunk(IFileChunkService fileChunkService)
        {
            _fileChunkService = fileChunkService ?? throw new ArgumentNullException(nameof(fileChunkService));
        }

        [FunctionName("FileChunk")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/chunk")] HttpRequest req,
            ILogger log,
            CancellationToken ct)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            IFormCollection formData = await req.ReadFormAsync(ct);

            if (formData == null)
            {
                return new BadRequestObjectResult(new { IsSuccess = false, Message = "No form found" });
            }

            if (!formData.Files.Any())
            {
                return new BadRequestObjectResult(new { IsSuccess = false, Message = "No file found" });
            }

            try
            {
                Guid fileId = new Guid(req.Query["fileId"].ToString());
                int index = int.Parse(req.Query["index"].ToString());
                var result = await _fileChunkService.SaveChunkAsync(fileId, index, formData.Files[0], ct);
                return new OkObjectResult(new { IsSuccess = true, Value = result });
            }
            catch (Exception e)
            {
                log.LogError(e, "Erreur dans la fonction FileChunk");
                return new OkObjectResult(new { IsSuccess = false });
            }
        }
    }
}
