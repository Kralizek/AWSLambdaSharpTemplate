using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;

namespace RichRequestResponseFunction
{
    public class ToUpperStringRequestResponseHandler : IRequestResponseHandler<string, string>
    {
        private readonly ILogger<ToUpperStringRequestResponseHandler> _logger;

        public ToUpperStringRequestResponseHandler(ILogger<ToUpperStringRequestResponseHandler> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _logger = logger;
        }

        public async Task<string> HandleAsync(string input, ILambdaContext context)
        {
            return input?.ToUpper();
        }
    }
}