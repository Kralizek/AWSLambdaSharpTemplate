using System;

namespace Kralizek.Lambda;

public interface IExecutionEnvironment
{
    string EnvironmentName { get; }

    bool IsLambda { get; }
}


public class LambdaExecutionEnvironment : IExecutionEnvironment
{
    internal const string DevelopmentEnvironmentName = "Development";
    internal const string ProductionEnvironmentName = "Production";
    
    public string EnvironmentName { get; init; } = DevelopmentEnvironmentName;

    public bool IsLambda { get; init; }
}

public static class ExecutionEnvironmentExtensions
{
    public static bool IsEnvironment(this IExecutionEnvironment executionEnvironment, string environmentName)
    {
        return string.Equals(executionEnvironment.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDevelopment(this IExecutionEnvironment executionEnvironment) => IsEnvironment(executionEnvironment, LambdaExecutionEnvironment.DevelopmentEnvironmentName);

    public static bool IsProduction(this IExecutionEnvironment executionEnvironment) => IsEnvironment(executionEnvironment, LambdaExecutionEnvironment.ProductionEnvironmentName);
}