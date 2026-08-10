# EventBridge sample

Use this sample when an EventBridge event has a strongly typed `detail` payload but the handler still needs the EventBridge envelope.

`Function` derives from `EventBridgeFunction<OrderCreated, OrderCreatedHandler>`. AWS deserialization materializes `CloudWatchEvent<OrderCreated>`, so the handler can work with `OrderCreated` while still reading envelope metadata such as source and detail type.

There is no separate payload decoder in this specialization because the Lambda serializer already constructs the typed EventBridge event.

## Look at

- `Function` for the EventBridge specialization.
- `OrderCreated` for the typed `detail` contract.
- `OrderCreatedHandler` for using both `input.Detail` and EventBridge envelope metadata.

For a service-neutral event with no EventBridge envelope, see [EventFunction](../EventFunction/).
