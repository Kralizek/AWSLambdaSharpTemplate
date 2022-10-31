using System;

namespace Kralizek.Lambda;

/// <summary>
/// A representation of the current execution environment
/// </summary>
public interface IExecutionEnvironment
{
    /// <summary>
    /// The name of the current environment as defined in the environment variables.
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// Specifies whether the current execution is in the Lambda runtime.
    /// </summary>
    bool IsLambda { get; }
}


internal class LambdaExecutionEnvironment : IExecutionEnvironment
{
    internal const string DevelopmentEnvironmentName = "Development";
    internal const string ProductionEnvironmentName = "Production";
    
    public string EnvironmentName { get; init; } = DevelopmentEnvironmentName;

    public bool IsLambda { get; init; }
}

/// <summary>
/// A set of extensions for <see cref="IExecutionEnvironment"/>.
/// </summary>
public static class ExecutionEnvironmentExtensions
{
    /// <summary>
    /// Checks whether the function is being executed in the environment specified by <paramref name="environmentName"/>.
    /// </summary>
    /// <param name="executionEnvironment">The current execution environment.</param>
    /// <param name="environmentName">The name of the execution environment to check.</param>
    public static bool IsEnvironment(this IExecutionEnvironment executionEnvironment, string environmentName)
    {
        return string.Equals(executionEnvironment.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether the function is being executed in the Development environment.
    /// </summary>
    /// <param name="executionEnvironment">The current execution environment.</param>
    public static bool IsDevelopment(this IExecutionEnvironment executionEnvironment) => IsEnvironment(executionEnvironment, LambdaExecutionEnvironment.DevelopmentEnvironmentName);

    /// <summary>
    /// Checks whether the function is being executed in the Production environment.
    /// </summary>
    /// <param name="executionEnvironment">The current execution environment.</param>
    public static bool IsProduction(this IExecutionEnvironment executionEnvironment) => IsEnvironment(executionEnvironment, LambdaExecutionEnvironment.ProductionEnvironmentName);
}