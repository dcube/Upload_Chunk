using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VideoChunk.Domain;
using VideoChunk.Services;

[assembly: FunctionsStartup(typeof(VideoChunk.Startup))]
namespace VideoChunk
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.Configure<StorageSettings>(configuration.GetSection("BlobStorage"));
            builder.Services.Configure<ApplicationSettings>(configuration.GetSection("Values"));

            builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();
            builder.Services.AddScoped<IFileChunkService, FileChunkService>();
        }
    }
}
