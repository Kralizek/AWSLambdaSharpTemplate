using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda;
using Kralizek.Lambda.Accessors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqsBatchResponseFunction;

public class Function : RequestResponseFunction<SQSEvent, SQSBatchResponse>
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
    private readonly ISqsRecordAccessor _accessor;

    public TestMessageHandler(ILogger<TestMessageHandler> logger, ISqsRecordAccessor accessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public Task HandleAsync(TestMessage? message, ILambdaContext context)
    {
        _logger.LogInformation("Received notification: {Message}", message?.Message);

        // show SQS details such as message id using ISqsRecordAccessor
        // ISqsRecordAccessor is not neccesary to take advantage of batch response mode
        _logger.LogInformation("Details from SQS about this message: {SqsRecord}", _accessor.SqsRecord);

        if (message is { Message.Length: > 0 })
        {
            if (message.Message.Contains("bad message", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("message supplied was a bad message", nameof(message));
            }
        }

        return Task.CompletedTask;
    }
}