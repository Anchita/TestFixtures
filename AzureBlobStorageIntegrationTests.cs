using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace IntegrationTests
{
    public class AzureBlobStorageIntegrationTests : IClassFixture<AzuriteTestFixture>
    {
        private readonly string? _blobConnectionString;
        private readonly AzuriteTestFixture _fixture;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly string testDataPath;

        public AzureBlobStorageIntegrationTests(AzuriteTestFixture fixture)
        {
            _fixture = fixture;

            _blobContainerClient = fixture.BlobContainerClient;
            _blobConnectionString = fixture.AzuriteConnectionString;

            testDataPath = fixture.TestDataFolderPath ?? throw new InvalidOperationException();
        }

        [Fact]
        public async Task Give__A_File_Is_Uploaded_To_AzureBlobStorage__When_The_File_Is_Downloaded__Then_The_FileContent_Is_Same_As_Original()
        {
            FileStream streamContent = File.OpenRead(
                Path.Combine(testDataPath, "TestFile.ia"));

            var filePath = "test/testFile.ia";
            BlobClient blobClient = _blobContainerClient.GetBlobClient(filePath);

            blobClient.Upload(streamContent, true);

            var readContent = await blobClient.DownloadContentAsync();

            Assert.Equal(readContent.Value.Content.ToArray(), File.ReadAllBytes(Path.Combine(testDataPath, "TestFile.ia")));

        }
    }
}
