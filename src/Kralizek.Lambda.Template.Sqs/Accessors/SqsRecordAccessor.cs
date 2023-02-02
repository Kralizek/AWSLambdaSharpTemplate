using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using static Amazon.Lambda.SQSEvents.SQSEvent;

namespace Kralizek.Lambda.Accessors
{
    namespace Internal
    {
        /// <summary>
        /// Provides access to information about the current SQS Message, if available.
        /// Provides SQS handlers with a way to set the current SQS Message.
        /// API Consumers should generally use <see cref="ISqsRecordAccessor" /> instead of using this class directly.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public class SqsRecordAccessor : ISqsRecordAccessor
        {
            internal SQSMessage? SQSMessage { get; set; }

            SqsRecord? ISqsRecordAccessor.SqsRecord => SQSMessage?.ToSqsRecord();
        }
    }

    /// <summary>
    /// Provides access to information about the current SQS Message, if available.
    /// Dependency-inject this into an implementation of <see cref="IMessageHandler{TMessage}" /> (or one of its dependencies)
    /// in order to inspect details of the raw SQS message (such as message id and receipt handle) that was presented to the Lambda for processing.
    /// </summary>
    /// <remarks>
    /// This accessor does not provide access to the parsed message body nor the raw body, and is not neccesary for most use cases.
    /// To read the parsed SQS message, see the message argument of <see cref="IMessageHandler{TMessage}.HandleAsync(TMessage?, Amazon.Lambda.Core.ILambdaContext)" />.
    /// Some example use cases where this accessor might be useful:
    ///  - Reading the SQS message id
    ///  - Using the receipt handle to customize retry delay (visibility timeout) in error cases
    /// </remarks>
    public interface ISqsRecordAccessor
    {
        /// <summary>
        /// Gets information about the current SQS Message, in the form of a <see cref="SqsRecord"/>.
        /// Returns null if there is no active SqsRecord.
        /// </summary>
        SqsRecord? SqsRecord { get; }
    }

    internal static class SqsMessageExtensions
    {
        public static SqsRecord ToSqsRecord(this SQSMessage message)
        {
            var record = new SqsRecord(message.MessageId, message.ReceiptHandle, message.Md5OfBody, message.EventSourceArn, message.EventSource, message.AwsRegion);
            record = record.WithAttributes(message.Attributes);
            return record;
        }

        public static void ExposeViaAccessor(this SQSMessage message, IServiceScope scope)
        {
            var accessor = scope.ServiceProvider.GetService<Internal.SqsRecordAccessor>();
            if (accessor is not null)
            {
                accessor.SQSMessage = message;
            }
        }
    }
}
