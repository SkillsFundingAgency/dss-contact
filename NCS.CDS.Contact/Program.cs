using Azure.Identity;
using DFC.HTTP.Standard;
using DFC.Swagger.Standard;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.SetBasePath(Environment.CurrentDirectory)
                        .AddJsonFile("local.settings.json", optional: true,
                            reloadOnChange: false)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;
                    services.AddOptions<ContactConfigurationSettings>()
                        .Bind(configuration);

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

                    services.AddSingleton(sp =>
                    {
                        var cosmosDbEndpoint = configuration["CosmosDbEndpoint"];
                        if (string.IsNullOrEmpty(cosmosDbEndpoint))
                        {
                            throw new InvalidOperationException("CosmosDbEndpoint is not configured.");
                        }

                        var options = new CosmosClientOptions() { ConnectionMode = ConnectionMode.Gateway };
                        return new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(), options);
                    });

                    services.AddSingleton(sp =>
                    {
                        var settings = sp.GetRequiredService<IOptions<ContactConfigurationSettings>>().Value;
                        settings.ServiceBusConnectionString =
                            $"Endpoint={settings.BaseAddress};SharedAccessKeyName={settings.KeyName};SharedAccessKey={settings.AccessKey}";

                        return new Azure.Messaging.ServiceBus.ServiceBusClient(settings.ServiceBusConnectionString);
                    });

                    services.Configure<LoggerFilterOptions>(options =>
                    {
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
