using System.Collections;
using System.Reflection;

namespace Risebird.Business.Tests.Infrastructure
{
    internal static class DefaultValueFactory
    {
        public static object Create(Type type)
        {
            if (type == null)
                return null;

            if (type.IsByRef)
                type = type.GetElementType()!;

            if (type == typeof(void))
                return null;

            if (Nullable.GetUnderlyingType(type) is Type underlying)
                return Create(underlying);

            if (type == typeof(string))
                return "test";

            if (type == typeof(bool))
                return true;

            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
                || type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(char))
                return Activator.CreateInstance(type);

            if (type == typeof(DateTime))
                return DateTime.UtcNow;

            if (type == typeof(DateTimeOffset))
                return DateTimeOffset.UtcNow;

            if (type == typeof(Guid))
                return Guid.NewGuid();

            if (type == typeof(TimeSpan))
                return TimeSpan.FromMinutes(1);

            if (type.IsEnum)
                return Enum.GetValues(type).GetValue(0);

            if (type.IsArray)
                return Array.CreateInstance(type.GetElementType()!, 0);

            if (type.IsInterface || type.IsAbstract)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (type.IsGenericType)
                    {
                        var args = type.GetGenericArguments();
                        var listType = typeof(List<>).MakeGenericType(args[0]);
                        return Activator.CreateInstance(listType);
                    }

                    return Array.Empty<object>();
                }

                return null;
            }

            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                if (def == typeof(List<>) || def == typeof(IList<>) || def == typeof(ICollection<>)
                    || def == typeof(IEnumerable<>) || def == typeof(IReadOnlyList<>) || def == typeof(IReadOnlyCollection<>))
                {
                    var listType = typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
                    return Activator.CreateInstance(listType);
                }

                if (def == typeof(Nullable<>))
                    return null;

                if (def == typeof(Task<>))
                    return null;

                if (def == typeof(ValueTuple<,>) || def == typeof(Tuple<,>))
                {
                    var args = type.GetGenericArguments().Select(Create).ToArray();
                    return Activator.CreateInstance(type, args);
                }
            }

            if (type == typeof(Task))
                return Task.CompletedTask;

            try
            {
                var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(c => c.GetParameters().Length)
                    .FirstOrDefault();

                if (ctor == null)
                    return type.IsValueType ? Activator.CreateInstance(type) : null;

                var ctorArgs = ctor.GetParameters().Select(p => Create(p.ParameterType)).ToArray();
                return ctor.Invoke(ctorArgs);
            }
            catch
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
        }
    }
}
