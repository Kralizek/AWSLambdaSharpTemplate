using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqsEventFunction;

public class Function : EventFunction<SQSEvent>
{
    protected override void Configure(IConfigurationBuilder builder)
    {
        builder.AddEnvironmentVariables();
    }

    protected override void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment)
    {
        logging.AddConfiguration(Configuration.GetSection("Logging"));

        logging.AddLambdaLogger(new LambdaLoggerOptions
        {
            IncludeCategory = true,
            IncludeLogLevel = true,
            IncludeNewline = true
        });
    }

    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
    {
        services.UseQueueMessageHandler<TestMessage, TestMessageHandler>();
    }
}

public class TestMessage
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class TestMessageHandler : IMessageHandler<TestMessage>
{
    private readonly ILogger<TestMessageHandler> _logger;

    public TestMessageHandler(ILogger<TestMessageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(TestMessage? message, ILambdaContext context)
    {
        _logger.LogInformation("Received notification: {Message}", message?.Message);

        return Task.CompletedTask;
    }
}