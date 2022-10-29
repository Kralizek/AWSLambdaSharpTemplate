[![Build status](https://ci.appveyor.com/api/projects/status/9usgike6oa0dg7wt?svg=true)](https://ci.appveyor.com/project/Kralizek/awslambdasharptemplate)

# Project description

Write complex AWS Lambda functions in C#, faster.

This project gives you a strong template that helps you write Lambda functions using C# and .NET Core without giving up on SOLID programming principles.

# Overview

This project is designed so that you can create a Lambda function like you create an ASP.NET Core web application.
The body of your function becomes your Startup class where the setup and the initialization happens.
The real logic of your function is nicely contained into an handler that acts as entry point.

This is what a minimal Lambda function will look like

```csharp
public class Function : EventFunction<string>
{
    protected override void Configure(IConfigurationBuilder builder)
    {

    }

    protected override void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment)
    {

    }

    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
    {
        RegisterHandler<EventHandler>(services);
    }
}
```

The only method required is `ConfigureServices` that you use to register your handler.
You can use `ConfigureLogging` to add log providers to the provided `ILoggingBuilder`.
`Configure` allows you to customize where your settings are taken from. Very much like ASP.NET Core web applications, you can use environment variables, configuration files and in-memory collections.

# Type of functions and handlers

AWS Lambda supports two types of invocation, `RequestResponse` and `Event`.
This distinction is mirrored by offering two distinct base classes.

## RequestResponse functions

A RequestResponse function represents a function that produces a return value that will be returned to the caller of the function.

To create a RequestResponse function, simply change your class so that it inherits from `RequestResponseFunction<TInput, TOutput>` where `TInput` is a class representing the incoming data and `TOutput` represents the value your function will produce.
Unless specific use cases, `TOutput` should always be your type, never `Task<TOutput>`.

A RequestResponse function requires an handler that implements `IRequestResponseHandler<TInput, TOutput>` to be registered in `ConfigureServices`.

This is the signature of `IRequestResponseHandler<TInput, TOutput>`

```csharp
public interface IRequestResponseHandler<TInput, TOutput>
{
    Task<TOutput> HandleAsync(TInput input, ILambdaContext context);
}
```

[Here](https://github.com/Kralizek/AWSLambdaSharpTemplate/tree/master/samples/RequestResponseFunction) you can find a sample that shows a RequestResponse function that returns the input string as upper case.

## Event functions

An Event function represents a function that is not expected to produce any value.

To create an Event function, simply change your class so that it inherits from `EventFunction<TInput>` where `TInput` is a class representing the incoming data.

An Event function requires an handler that implements `IEventHandler<TInput>` to be registered in `ConfigureServices`.

This is the signature of `IEventHandler<TInput>`

```csharp
public interface IEventHandler<TInput>
{
    Task HandleAsync(TInput input, ILambdaContext context);
}
```

[Here](https://github.com/Kralizek/AWSLambdaSharpTemplate/tree/master/samples/EventFunction) you can find a sample that shows an Event function that accepts an input string and logs it into CloudWatch logs.

The library offers special support for two classes of Event functions: functions that handle SQS and SNS messages.

### Event functions handling SNS notifications

AWS Lambda functions can be used to handle SNS notifications.

Since all SNS notifications have the same structure, the package `Kralizek.Lambda.Template.Sns` can be used to speed up the development of functions that handle SNS notifications.

To do so, create a class that implements the interface `INotificationHandler<MyNotification>`, then change the `ConfigureServices` method to look like the following snippet.

```csharp
protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
{
    services.UseNotificationHandler<MyNotification, MyNotificationHandler>();
}
```

#### Parallel execution

Since a single SNS request can contain multiple messages, you can specify that you want all messages to be processed in parallel by changing the `ConfigureServices` method like in the snippet below.

```csharp
protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
{
    services.UseNotificationHandler<MyNotification, MyNotificationHandler>().WithParallelExecution(maxDegreeOfParallelism: 4);
}
```

You can use the `maxDegreeOfParallelism` parameter to specify the amount of parallel executions that you desire. By default, the amount of logical processors available is used.

```csharp
protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
{
    services.UseNotificationHandler<MyNotification, MyNotificationHandler>().WithParallelExecution();
}
```

#### Custom serialization

Since each SNS message contains the actual payload as encoded a string, a custom serializer can be specified to replace the default JSON serializer.

To do so, create an implementation of the interface `INotificationSerializer` and register it in the `ConfigureServices` method.

```csharp
public class MyCustomSerializer : INotificationSerializer
{
  public TMessage? Deserialize<TMessage>(string input)
  {
    // implement your deserialization strategy here
  }
}
```

```csharp
protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
{
    services.UseNotificationHandler<MyNotification, MyNotificationHandler>();

    services.UseCustomNotificationSerializer<MyCustomSerializer>();
}
```

### Event functions handling SQS messages

# Creating a new function

The best way to create a new AWS Lambda that uses this structure is to use the `dotnet new` template provided via NuGet.

1. Ensure you have the latest [.NET Core SDK](https://www.microsoft.com/net/download/core) installed.
2. Open your console prompt of choice and type `dotnet new -i "Kralizek.Lambda.Templates"`. This will install the latest version of the templates. Depending on your previous usage of `dotnet new`, it might take some time.
3. List all available templates by typing `dotnet new -all`. You will see 4 new entries, all starting with Lambda.
4. Create a new project using the template of your choice by typing `dotnet new {short name of the template} -n NameOfYourProject`. E.g. `dotnet new lambda-template-event-empty -n Sample`
5. Start hacking!

Here is a list of all the available templates

|Name|Short name|Description|
|-|-|-|
|Lambda Empty Event Function|lambda-template-event-empty|Creates an Event function with no extra setup|
|Lambda Empty RequestResponse Function|lambda-template-requestresponse-empty|Creates a RequestResponse function with no extra setup|
|Lambda Boilerplate Event Function|lambda-template-event-boilerplate|Creates an Event function with some boilerplate added|
|Lambda Boilerplate RequestResponse Function|lambda-template-requestresponse-boilerplate|Creates a RequestResponse function with some boilerplate added|

All the templates support the following parameters

* `--name|-n` Name of the project. It is also used as name of the function
* `--role` Name of the IAM role the function will use when being executed
* `--region` Name of the AWS region where the function will be installed
* `--profile` Name of the profile whose credentials will be used when interacting with AWS.

## Empty templates

The empty templates are created with just the minimum required dependencies.

These include:

* `Amazon.Lambda.Core`
* `Amazon.Lambda.Serialization.SystemTextJson`
* `Kralizek.Lambda.Template`
* `Amazon.Lambda.Tools`

## Boilerplate templates

The boilerplate templates are an enriched version of the empty templates. They contain some extra setup already wired up for you.

* Logs are automatically pushed to CloudWatch.
* Settings are loaded from environment variables and a `appsettings.json` file attached to the project.

Besides the basic dependencies of the empty templates, the boilerplate templates have some extra dependencies.

The extra dependencies are:

* `Amazon.Lambda.Serialization.SystemTextJson` is used to push logs into AWS CloudWatch
* `Microsoft.Extensions.Configuration.EnvironmentVariables` used to load configuration values from environment variables
* `Microsoft.Extensions.Configuration.Json` used to load configuration values from json files

## Tools

Since the templates are based off the original ones provided by Amazon, you can use `dotnet lambda`-commands to package/create/deploy/execute your function.
