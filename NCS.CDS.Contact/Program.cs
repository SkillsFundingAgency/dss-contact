using DFC.HTTP.Standard;
using DFC.Swagger.Standard;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Containers;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Cosmos.Services;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.ServiceBus;
using NCS.DSS.Contact.Validation;

namespace NCS.DSS.Contact
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var contactConfigurationSettings = new ContactConfigurationSettings()
            {
                Endpoint = Environment.GetEnvironmentVariable("Endpoint") ?? throw new ArgumentNullException(),
                Key = Environment.GetEnvironmentVariable("Key") ?? throw new ArgumentNullException(),
                KeyName = Environment.GetEnvironmentVariable("KeyName") ?? throw new ArgumentNullException(),
                AccessKey = Environment.GetEnvironmentVariable("AccessKey") ?? throw new ArgumentNullException(),
                BaseAddress = Environment.GetEnvironmentVariable("BaseAddress") ?? throw new ArgumentNullException(),
                QueueName = Environment.GetEnvironmentVariable("QueueName") ?? throw new ArgumentNullException(),
                ContactDetailsConnectionString = Environment.GetEnvironmentVariable("ContactDetailsConnectionString") 
                                                 ?? throw new ArgumentNullException(),
                ServiceBusConnectionString = string.Empty,
                DatabaseId = Environment.GetEnvironmentVariable("DatabaseId") ?? throw new ArgumentNullException(),
                CollectionId = Environment.GetEnvironmentVariable("CollectionId") ?? throw new ArgumentNullException(),
                CustomerDatabaseId = Environment.GetEnvironmentVariable("CustomerDatabaseId") ?? throw new ArgumentNullException(),
                CustomerCollectionId = Environment.GetEnvironmentVariable("CustomerCollectionId") ?? throw new ArgumentNullException(),
                DigitalIdentityDatabaseId = Environment.GetEnvironmentVariable("DigitalIdentityDatabaseId") ?? throw new ArgumentNullException(),
                DigitalIdentityCollectionId = Environment.GetEnvironmentVariable("DigitalIdentityCollectionId") ?? throw new ArgumentNullException(),
                SearchServiceIndexName = Environment.GetEnvironmentVariable("CustomerSearchIndexName") ?? throw new ArgumentNullException(),
                SearchServiceKey = Environment.GetEnvironmentVariable("SearchServiceAdminApiKey") ?? throw new ArgumentNullException(),
                SearchServiceName = Environment.GetEnvironmentVariable("SearchServiceName") ?? throw new ArgumentNullException()
            };
            contactConfigurationSettings.ServiceBusConnectionString =
                $"Endpoint={contactConfigurationSettings.BaseAddress}" +
                $";SharedAccessKeyName={contactConfigurationSettings.KeyName}" +
                $";SharedAccessKey={contactConfigurationSettings.AccessKey}";

            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();

                    services.AddLogging();

                    services.AddTransient<IServiceBusClient, ServiceBusClient>();
                    services.AddTransient<ICosmosDBProvider, CosmosDBProvider>();
                    services.AddTransient<IGetContactHttpTriggerService, GetContactHttpTriggerService>();
                    services.AddTransient<IGetContactDetailsByIdHttpTriggerService, GetContactDetailsByIdHttpTriggerService>();
                    services.AddTransient<IPostContactDetailsHttpTriggerService, PostContactDetailsHttpTriggerService>();
                    services.AddTransient<IPatchContactDetailsHttpTriggerService, PatchContactDetailsHttpTriggerService>();
                    services.AddTransient<IResourceHelper, ResourceHelper>();
                    services.AddTransient<IValidate, Validate>();
                    services.AddTransient<IHttpRequestHelper, HttpRequestHelper>();

                    services.AddSingleton<ISearchService, SearchService>();
                    services.AddSingleton<ISwaggerDocumentGenerator, SwaggerDocumentGenerator>();
                    services.AddSingleton<IConvertToDynamic, ConvertToDynamic>();

                    services.AddSingleton(contactConfigurationSettings);

                    services.AddSingleton(s =>
                    {
                        var options = new CosmosClientOptions()
                        {
                            ConnectionMode = ConnectionMode.Gateway
                        };

                        var cosmosClient = new CosmosClient(contactConfigurationSettings.ContactDetailsConnectionString, options);

                        cosmosClient.GetContainer(
                            contactConfigurationSettings.DatabaseId,
                            contactConfigurationSettings.CollectionId
                        );

                        cosmosClient.GetContainer(
                            contactConfigurationSettings.CustomerDatabaseId,
                            contactConfigurationSettings.CustomerCollectionId
                        );

                        cosmosClient.GetContainer(
                            contactConfigurationSettings.DigitalIdentityDatabaseId,
                            contactConfigurationSettings.DigitalIdentityCollectionId
                        );

                        return cosmosClient;
                    });

                    services.AddSingleton<IContactContainer>(s =>
                    {
                        var cosmosClient = s.GetRequiredService<CosmosClient>();
                        return new ContactContainer(cosmosClient.GetContainer(
                            contactConfigurationSettings.DatabaseId,
                            contactConfigurationSettings.CollectionId
                        ));
                    });

                    services.AddSingleton<ICustomerContainer>(s =>
                    {
                        var cosmosClient = s.GetRequiredService<CosmosClient>();
                        return new CustomerContainer(cosmosClient.GetContainer(
                            contactConfigurationSettings.CustomerDatabaseId,
                            contactConfigurationSettings.CustomerCollectionId
                        ));
                    });

                    services.AddSingleton<IDigitalIdentityContainer>(s =>
                    {
                        var cosmosClient = s.GetRequiredService<CosmosClient>();
                        return new DigitalIdentityContainer(cosmosClient.GetContainer(
                            contactConfigurationSettings.DigitalIdentityDatabaseId,
                            contactConfigurationSettings.DigitalIdentityCollectionId
                        ));
                    });

                    services.AddSingleton(s => new Azure.Messaging.ServiceBus.ServiceBusClient(contactConfigurationSettings.ServiceBusConnectionString));

                    services.Configure<LoggerFilterOptions>(options =>
                    {
                        // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
                        // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
                        LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                            == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
                        if (toRemove is not null)
                        {
                            options.Rules.Remove(toRemove);
                        }
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}
