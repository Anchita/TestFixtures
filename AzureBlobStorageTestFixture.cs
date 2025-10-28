using Azure.Storage.Blobs;
using Testcontainers.Azurite;

namespace IntegrationTests
{
    public class AzuriteTestFixture : IAsyncLifetime
    {
        public string? AzuriteConnectionString { get; protected set; }
        public required string TestDataFolderPath;
        public required BlobContainerClient BlobContainerClient;
        private readonly AzuriteContainer _azuriteContainer;

        public AzuriteTestFixture()
        {
            _azuriteContainer = new AzuriteBuilder()
            .WithCommand("--skipApiVersionCheck")
            .Build();

            string currentDirectory = Directory.GetCurrentDirectory();
            string parentFolder1 = Directory.GetParent(currentDirectory)?.ToString() ?? throw new InvalidOperationException("Parent directory is null.");
            string parentFolder2 = Directory.GetParent(parentFolder1)?.ToString() ?? throw new InvalidOperationException("Parent directory is null.");
            TestDataFolderPath = Directory.GetParent(parentFolder2)?.ToString() ?? throw new InvalidOperationException("Parent directory is null.");
        }

        public async Task InitializeAsync()
        {
            try
            {

                await _azuriteContainer.StartAsync();

                AzuriteConnectionString = _azuriteContainer.GetConnectionString();

                BlobContainerClient = new BlobContainerClient(AzuriteConnectionString, "test");

                await BlobContainerClient.CreateIfNotExistsAsync();

            }
            catch
            {
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            try
            {
                await _azuriteContainer.StopAsync();
            }

            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during DisposeAsync: {ex.Message}");
            }

        }
    }
}
