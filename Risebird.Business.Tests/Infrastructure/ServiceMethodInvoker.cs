using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Risebird.Business.Tests.Infrastructure
{
    internal static class ServiceMethodInvoker
    {
        public static IEnumerable<MethodInfo> GetPublicInstanceMethods(Type serviceType) =>
            serviceType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => !m.IsGenericMethodDefinition)
                .OrderBy(m => m.Name)
                .ThenBy(m => m.ToString());

        public static string GetMethodKey(MethodInfo method) =>
            $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";

        public static async Task InvokeAsync(object sut, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.IsOut || parameter.ParameterType.IsByRef)
                {
                    var elementType = parameter.ParameterType.GetElementType()!;
                    args[i] = DefaultValueFactory.Create(elementType);
                }
                else if (parameter.HasDefaultValue)
                {
                    args[i] = parameter.DefaultValue;
                }
                else
                {
                    args[i] = DefaultValueFactory.Create(parameter.ParameterType);
                }
            }

            object result;
            try
            {
                result = method.Invoke(sut, args);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                throw;
            }

            if (result is Task task)
                await task.ConfigureAwait(false);
        }
    }
}
