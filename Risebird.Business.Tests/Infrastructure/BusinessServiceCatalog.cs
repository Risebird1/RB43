using System.Reflection;

namespace Risebird.Business.Tests.Infrastructure
{
    /// <summary>
    /// Reflection over the Risebird.Business assembly: which service types exist, and which of
    /// their public methods the coverage smoke suite drives. Keeping discovery here means a
    /// newly added service is picked up by the suite automatically.
    /// </summary>
    internal static class BusinessServiceCatalog
    {
        public static readonly Assembly BusinessAssembly = typeof(InterviewerService).Assembly;

        /// <summary>
        /// Types the smoke suite must not construct or drive. Add a type here only with a
        /// reason — anything excluded is coverage we are choosing to give up.
        /// </summary>
        private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal)
        {
            // Data-only types that live in Risebird.Business but are not services. These expose
            // no behaviour for the smoke suite to drive, so excluding them gives up no coverage.
            "CodeProjectStack",
        };

        /// <summary>Every concrete, publicly constructible service in Risebird.Business.</summary>
        public static IEnumerable<Type> ServiceTypes() =>
            BusinessAssembly
                .GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && !t.IsNested)
                .Where(t => !Excluded.Contains(t.Name))
                .Where(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
                .OrderBy(t => t.Name, StringComparer.Ordinal);

        /// <summary>Public instance methods declared on the service itself (not inherited).</summary>
        public static IEnumerable<MethodInfo> MethodsOf(Type serviceType) =>
            ServiceMethodInvoker.GetPublicInstanceMethods(serviceType);

        /// <summary>Stable "Service.Method(ParamTypes)" key used as the theory discriminator.</summary>
        public static string KeyFor(Type serviceType, MethodInfo method) =>
            $"{serviceType.Name}.{ServiceMethodInvoker.GetMethodKey(method)}";

        public static IEnumerable<object[]> ServiceTypeNames() =>
            ServiceTypes().Select(t => new object[] { t.Name });

        public static Type ResolveServiceType(string typeName) =>
            ServiceTypes().Single(t => t.Name == typeName);
    }
}
