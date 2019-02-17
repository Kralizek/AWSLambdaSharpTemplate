using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.Lambda.SNSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SnsEventFunction
{
    public class Function : EventFunction<SNSEvent>
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
            services.UseNotificationHandler<CustomNotification, CustomNotificationHandler>();
        }
    }

    public class CustomNotification
    {
        public string Message { get; set; }
    }

    public class CustomNotificationHandler : INotificationHandler<CustomNotification>
    {
        private readonly ILogger<CustomNotificationHandler> _logger;

        public CustomNotificationHandler(ILogger<CustomNotificationHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleAsync(CustomNotification notification, ILambdaContext context)
        {
            _logger.LogInformation($"Handling notification: {notification.Message}");

            return Task.CompletedTask;
        }
    }
}
