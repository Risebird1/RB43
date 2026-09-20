using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Risebird.Business.Tests.Infrastructure
{
    internal static class ServiceMethodInvoker
    {
        /// <summary>How long one auto-mocked method call may take before it is treated as hung.</summary>
        public static readonly TimeSpan InvocationBudget = TimeSpan.FromSeconds(10);

        public static IEnumerable<MethodInfo> GetPublicInstanceMethods(Type serviceType) =>
            serviceType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => !m.IsGenericMethodDefinition)
                .OrderBy(m => m.Name)
                .ThenBy(m => m.ToString());

        public static string GetMethodKey(MethodInfo method) =>
            $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";

        /// <summary>
        /// Invokes <paramref name="method"/> with default arguments and rethrows whatever escapes.
        /// Kept for the hand-written suites that assert a method is exception-free.
        /// </summary>
        public static async Task InvokeAsync(object sut, MethodInfo method) =>
            await InvokeAsync(sut, method, autoMocker: null).ConfigureAwait(false);

        /// <summary>
        /// Invokes <paramref name="method"/>, using <paramref name="autoMocker"/> (when supplied)
        /// to build richer argument values than <see cref="DefaultValueFactory"/> alone.
        /// </summary>
        public static async Task InvokeAsync(object sut, MethodInfo method, AutoMocker autoMocker) =>
            await InvokeAsync(sut, method, autoMocker, argumentFactory: null).ConfigureAwait(false);

        /// <summary>
        /// As above, but <paramref name="argumentFactory"/> (when supplied) chooses the argument
        /// values — letting a sweep drive the same method with populated input rather than the
        /// harmless defaults <see cref="DefaultValueFactory"/> produces.
        /// </summary>
        public static async Task InvokeAsync(
            object sut, MethodInfo method, AutoMocker autoMocker, Func<Type, string, object> argumentFactory)
        {
            object result;
            try
            {
                result = method.Invoke(sut, BuildArguments(method, autoMocker, argumentFactory));
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                throw;
            }

            if (result is Task task)
                await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Drives one method and reports the outcome instead of throwing, so a coverage sweep can
        /// continue past a method that is not defensive about default input.
        /// </summary>
        public static async Task<InvocationOutcome> TryInvokeAsync(
            object sut, MethodInfo method, AutoMocker autoMocker, string key,
            Func<Type, string, object> argumentFactory = null)
        {
            var invocation = Task.Run(async () =>
                await InvokeAsync(sut, method, autoMocker, argumentFactory).ConfigureAwait(false));
            var timeout = Task.Delay(InvocationBudget);

            var finished = await Task.WhenAny(invocation, timeout).ConfigureAwait(false);
            if (finished != invocation)
                return new InvocationOutcome { Key = key, TimedOut = true };

            try
            {
                await invocation.ConfigureAwait(false);
                return new InvocationOutcome { Key = key };
            }
            catch (Exception ex)
            {
                return new InvocationOutcome { Key = key, Exception = ex };
            }
        }

        private static object[] BuildArguments(
            MethodInfo method, AutoMocker autoMocker, Func<Type, string, object> argumentFactory)
        {
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.IsOut || parameter.ParameterType.IsByRef)
                {
                    args[i] = Build(parameter.ParameterType.GetElementType(), parameter.Name);
                    continue;
                }

                if (parameter.ParameterType == typeof(CancellationToken))
                {
                    args[i] = CancellationToken.None;
                    continue;
                }

                // An optional parameter's own default is the faithful choice for the baseline
                // sweep. A sweep that supplies a factory wants the other side of those switches,
                // so it overrides the default rather than accepting it.
                if (parameter.HasDefaultValue && argumentFactory == null)
                {
                    args[i] = parameter.DefaultValue;
                    continue;
                }

                if (autoMocker != null && parameter.ParameterType.IsInterface)
                {
                    args[i] = autoMocker.Resolve(parameter.ParameterType);
                    continue;
                }

                args[i] = Build(parameter.ParameterType, parameter.Name);
            }

            return args;

            object Build(Type type, string memberName)
            {
                if (argumentFactory == null)
                    return DefaultValueFactory.Create(type);

                try { return argumentFactory(type, memberName) ?? DefaultValueFactory.Create(type); }
                catch { return DefaultValueFactory.Create(type); }
            }
        }
    }
}
