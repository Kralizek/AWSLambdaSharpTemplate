using System;
using System.Collections.Generic;

namespace Kralizek.Lambda.Accessors
{
    /// <summary>
    /// Immutable and abridged representation of <see cref="Amazon.Lambda.SQSEvents.SQSEvent.SQSMessage" />.
    /// <see href="https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html"/> shows example Lambda payloads for SQS-triggered cases,
    /// including attributes such as <see cref="ApproximateReceiveCount" />.
    /// </summary>
    public record class SqsRecord(string MessageId, string ReceiptHandle, string? Md5OfBody, string? EventSourceArn, string? EventSource, string? AwsRegion)
    {
        #region SQS Attributes
        public long ApproximateReceiveCount { get; init; }

        public DateTimeOffset SentTimestamp { get; init; }

        public string? SenderId { get; init; }

        public DateTimeOffset ApproximateFirstReceiveTimestamp { get; init; }
        #endregion

        #region SQS FIFO Attributes
        public string? SequenceNumber { get; init; }

        public string? MessageGroupId { get; init; }

        public string? MessageDeduplicationId { get; init; }
        #endregion

        internal SqsRecord WithAttributes(IReadOnlyDictionary<string, string> attributes)
        {
            SqsRecord record = this;

            if (attributes is null)
            {
                return record;
            }
            
            SetInteger("ApproximateReceiveCount", value => record = record with { ApproximateReceiveCount = value });
            SetTimestamp("SentTimestamp", value => record = record with { SentTimestamp = value });
            SetString("SenderId", value => record = record with { SenderId = value });
            SetTimestamp("ApproximateFirstReceiveTimestamp", value => record = record with { ApproximateFirstReceiveTimestamp = value });
            SetString("SequenceNumber", value => record = record with { SequenceNumber = value });
            SetString("MessageGroupId", value => record = record with { MessageGroupId = value });
            SetString("MessageDeduplicationId", value => record = record with { MessageDeduplicationId = value });
            return record;

            void SetString(string key, Action<string> actor)
            {
                if (attributes.TryGetValue(key, out var value) && value is not null)
                {
                    actor(value);
                }
            }
            void SetInteger(string key, Action<long> actor) =>
                SetString(key, str => actor(Convert.ToInt64(str)));
            void SetTimestamp(string key, Action<DateTimeOffset> actor) =>
                SetInteger(key, number => actor(DateTimeOffset.FromUnixTimeMilliseconds(number)));
        }
    }
}
