using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.ServiceBus;

namespace IntegrationTests
{
    public class AzureServiceBusEmulatorTestFixture : IAsyncLifetime
    {
        public IContainer? EmulatorContainer { get; protected set; }
        public IContainer? SqlEdgeContainer { get; protected set; }
        public INetwork? Network { get; protected set; }
        public string? ServiceBusConnectionString { get; protected set; }
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private readonly ServiceBusContainer _serviceBusContainer;
        public const ushort ServiceBusPort = 5672;
        public const ushort ServiceBusHttpPort = 5300;

        public ILoggerFactory GetLoggerFactory()
        {
            return _loggerFactory;
        }

        private string? emulatorConfigFilePath = null;

        private const string emulatorConfig = @"{
            ""UserConfig"": {
                ""Namespaces"": [
                    {
                        ""Name"": ""sbemulatorns"",
                        ""Queues"": [
                            {
                                ""Name"": ""myqueue"",
                                ""Properties"": {
                                    ""DeadLetteringOnMessageExpiration"": false,
                                    ""DefaultMessageTimeToLive"": ""PT1H"",
                                    ""LockDuration"": ""PT1M"",
                                    ""MaxDeliveryCount"": 10,
                                    ""RequiresDuplicateDetection"": false,
                                    ""RequiresSession"": false
                                }
                            }
                        ]
                    }
                ],
                ""Logging"": {
                    ""Type"": ""File""
                }
            }
        }";

        public AzureServiceBusEmulatorTestFixture()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = _loggerFactory.CreateLogger<AzureServiceBusEmulatorTestFixture>();

            emulatorConfigFilePath = Path.GetTempFileName();
            File.WriteAllText(emulatorConfigFilePath, emulatorConfig);

            _serviceBusContainer = new ServiceBusBuilder()
                .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
                .WithAcceptLicenseAgreement(true)
                .WithPortBinding(ServiceBusPort, true)
                .WithPortBinding(ServiceBusHttpPort, true)
                .WithEnvironment("SQL_WAIT_INTERVAL", "0")
                .WithBindMount(emulatorConfigFilePath, "/ServiceBus_Emulator/ConfigFiles/Config.json")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request =>
                    request.ForPort(ServiceBusHttpPort).ForPath("/health")))
                .Build();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _serviceBusContainer.StartAsync();

                await Task.Delay(5000); // Additional wait to ensure services are fully up

                ServiceBusConnectionString = GetConnectionString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during test fixture initialization");
                throw;
            }
        }

        public string GetConnectionString()
        {
            var hostPort = _serviceBusContainer.GetMappedPublicPort(ServiceBusPort);
            return $"Endpoint=sb://localhost:{hostPort}/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey==SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        }

        public async Task DisposeAsync()
        {
            try
            {
                if (EmulatorContainer != null)
                {
                    await _serviceBusContainer.StopAsync();
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during DisposeAsync: {ex.Message}");
            }

            finally
            {
                if (emulatorConfigFilePath != null && File.Exists(emulatorConfigFilePath))
                {
                    File.Delete(emulatorConfigFilePath);
                }
            }
        }
    }
}
