using System;

namespace Kralizek.Lambda
{
    public interface IExecutionEnvironment
    {
        string EnvironmentName { get; }

        bool IsLambda { get; }
    }


    public class LambdaExecutionEnvironment : IExecutionEnvironment
    {
        public string EnvironmentName { get; set; }

        public bool IsLambda { get; set; }
    }

    public static class ExecutionEnvironmentExtensions
    {
        public static bool IsEnvironment(this IExecutionEnvironment executionEnvironment, string environmentName)
        {
            return string.Equals(executionEnvironment.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDevelopment(this IExecutionEnvironment executionEnvironment) => executionEnvironment.EnvironmentName == null || IsEnvironment(executionEnvironment, "development");

        public static bool IsProduction(this IExecutionEnvironment executionEnvironment) => IsEnvironment(executionEnvironment, "production");
    }
}