using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using cldvPart1.Models;

namespace cldvPart1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _connectionString = "DefaultEndpointsProtocol=https;AccountName=olebogengdibodusa;AccountKey=3y+w5OzaOENZq8+/QveYT5N5guZ4pghSm/tJdm176NO90KuL5wGE33KkSF1kzfm7wd57DAVx1K7c+AStJ9hdNg==;EndpointSuffix=core.windows.net";
        private readonly string _tableName = "consumerProfile";
        private readonly string _queueName = "consumerqueue";
        private readonly string _fileShareName = "consumerfileshare";
        private readonly string _blobContainerName = "consumerblob";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult abcRetail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> abcRetail([FromBody] CustomerProfile customerProfile)
        {
            try
            {
                // Insert or update the customer profile in the Azure Table Storage
                var tableClient = new TableClient(_connectionString, _tableName);
                customerProfile.RowKey = Guid.NewGuid().ToString(); // Set a unique RowKey
                await tableClient.UpsertEntityAsync(customerProfile);

                // Add a message to the Azure Queue Storage
                var queueClient = new QueueClient(_connectionString, _queueName);
                await queueClient.CreateIfNotExistsAsync();
                string message = $"Customer profile created/updated: {customerProfile.Name}";
                await queueClient.SendMessageAsync(message);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customer profile.");
                return StatusCode(500, "Internal server error.");
            }
        }

        public async Task<IActionResult> DeleteCustomerProfile(string partitionKey, string rowKey)
        {
            try
            {
                var tableClient = new TableClient(_connectionString, _tableName);
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
                return Ok("Customer profile deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer profile.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Upload Image/Contract/Log File to Azure Blob Storage
        [HttpPost]
        public async Task<IActionResult> UploadImage()
        {
            var file = Request.Form.Files[0];
            if (file != null && file.Length > 0)
            {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
                await blobContainerClient.CreateIfNotExistsAsync();

                var blobClient = blobContainerClient.GetBlobClient(file.FileName);
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                return Ok("File uploaded.");
            }

            return BadRequest("File not uploaded.");
        }

        // Add Message to Azure Queue Storage
        public async Task<IActionResult> AddMessageToQueue()
        {
            var queueClient = new QueueClient(_connectionString, _queueName);
            await queueClient.CreateIfNotExistsAsync();

            string message = "Processing consumer data";
            await queueClient.SendMessageAsync(message);

            return Ok("Message added to queue.");
        }

        // Upload File to Azure File Share
        [HttpPost]
        public async Task<IActionResult> UploadFileToShare()
        {
            var file = Request.Form.Files[0];
            if (file != null && file.Length > 0)
            {
                var shareClient = new ShareClient(_connectionString, _fileShareName);
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(file.FileName);
                using (var stream = file.OpenReadStream())
                {
                    await fileClient.CreateAsync(stream.Length);
                    await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
                }

                return Ok("File uploaded to share.");
            }

            return BadRequest("File not uploaded.");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}