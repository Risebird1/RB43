using System.Collections;
using System.Reflection;

namespace Risebird.Business.Tests.Infrastructure
{
    /// <summary>
    /// Builds a *populated* instance of an arbitrary type: non-empty collections, filled
    /// reference properties, and scalars leaning whichever way the <see cref="PopulationProfile"/>
    /// asks for.
    ///
    /// <see cref="DefaultValueFactory"/> answers "what is a harmless value of this type" and so
    /// pushes a service down its empty-input path. This one answers "what does a realistic
    /// repository hand back", which is what walks the loop bodies, the mapping code and the
    /// branch-taken side of the guards — the bulk of the business layer.
    ///
    /// The graph is deliberately bounded. Entity view models reference each other freely, so an
    /// unbounded walk either never terminates or builds tens of thousands of objects per call:
    /// <see cref="Builder.MaxDepth"/> caps how far complex properties are followed, a type already
    /// on the current path is not re-entered, and only Risebird-owned types are recursed into.
    /// </summary>
    internal static class PopulatedValueFactory
    {
        public static object Create(Type type) => Create(type, PopulationProfile.Affirmative);

        public static object Create(Type type, PopulationProfile profile) =>
            Create(type, profile, memberName: null);

        /// <param name="memberName">
        /// Name of the parameter or property this value is for. Worth passing wherever it is
        /// known: a method that takes <c>financialYear</c> and parses it will throw on "test"
        /// before running any of the logic the sweep is trying to reach.
        /// </param>
        public static object Create(Type type, PopulationProfile profile, string memberName) =>
            new Builder(profile).Build(type, depth: 0, memberName: memberName);

        private sealed class Builder
        {
            /// <summary>How many levels of complex (class) properties are followed.</summary>
            private const int MaxDepth = 2;

            /// <summary>Elements per generated collection — two, so "more than one" logic runs.</summary>
            private const int CollectionSize = 2;

            private readonly PopulationProfile _profile;
            private readonly HashSet<Type> _onPath = new();

            public Builder(PopulationProfile profile) => _profile = profile;

            /// <param name="memberName">
            /// The property or parameter this value is destined for, when known. Only strings use
            /// it, and only to pick a plausible shape: a member called <c>WebApiUrl</c> filled
            /// with "test" makes <c>new Uri(...)</c> throw, which stops the sweep at the
            /// constructor instead of reaching the code it came to cover. The same is true of the
            /// e-mail, JSON and phone parsing scattered through the business layer.
            /// </param>
            public object Build(Type type, int depth, string memberName)
            {
                if (type == null || type == typeof(void))
                    return null;

                if (type.IsByRef)
                    type = type.GetElementType();

                if (Nullable.GetUnderlyingType(type) is Type underlying)
                    return Build(underlying, depth, memberName);

                if (type == typeof(string))
                    return StringFor(memberName);

                if (TryBuildScalar(type, out var scalar))
                    return scalar;

                if (type == typeof(Task))
                    return Task.CompletedTask;

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = type.GetGenericArguments()[0];
                    var inner = Build(resultType, depth, memberName);
                    return typeof(Task).GetMethod(nameof(Task.FromResult))
                        .MakeGenericMethod(resultType)
                        .Invoke(null, new[] { inner });
                }

                if (TryBuildTuple(type, depth, memberName, out var tuple))
                    return tuple;

                if (TryBuildCollection(type, depth, memberName, out var collection))
                    return collection;

                // Interfaces and abstracts are collaborators, not data — the Moq provider handles
                // those. Anything else is a POCO worth filling in.
                if (type.IsInterface || type.IsAbstract)
                    return null;

                return BuildPoco(type, depth);
            }

            private bool TryBuildScalar(Type type, out object value)
            {
                value = type switch
                {
                    _ when type == typeof(bool) => _profile.BooleanValue,
                    _ when type == typeof(char) => 'x',
                    _ when type == typeof(DateTime) => new DateTime(2026, 01, 15, 09, 30, 00, DateTimeKind.Utc),
                    _ when type == typeof(DateTimeOffset) => new DateTimeOffset(2026, 01, 15, 09, 30, 00, TimeSpan.Zero),
                    _ when type == typeof(TimeSpan) => TimeSpan.FromMinutes(30),
                    _ when type == typeof(Guid) => Guid.NewGuid(),
                    _ when type == typeof(decimal) => (decimal)_profile.NumericValue,
                    _ when type == typeof(double) => (double)_profile.NumericValue,
                    _ when type == typeof(float) => (float)_profile.NumericValue,
                    _ when type.IsPrimitive => ConvertNumeric(type, _profile.NumericValue),
                    _ when type.IsEnum => EnumValue(type),
                    _ => null
                };

                return value != null;
            }

            private static object ConvertNumeric(Type type, int value)
            {
                try { return Convert.ChangeType(value, type); }
                catch { return Activator.CreateInstance(type); }
            }

            private object EnumValue(Type type)
            {
                var values = Enum.GetValues(type);
                if (values.Length == 0)
                    return Activator.CreateInstance(type);

                var index = Math.Clamp(_profile.EnumIndex, 0, values.Length - 1);
                return values.GetValue(index);
            }

            /// <summary>
            /// A plausible string for <paramref name="memberName"/>. The business layer parses
            /// URLs into <see cref="Uri"/>, splits e-mail lists and deserializes JSON columns; a
            /// literal "test" throws in all three places before the method under sweep does any
            /// work.
            /// </summary>
            private string StringFor(string memberName)
            {
                if (_profile.BlankStrings)
                    return string.Empty;

                if (string.IsNullOrEmpty(memberName))
                    return "test";

                var name = memberName.ToLowerInvariant();

                // Domain formats the business layer parses rather than just passes along. These
                // are checked first because several also contain a generic token ("financialYear"
                // contains "year"), and because a service that throws while parsing its own
                // arguments never reaches the code the sweep exists to cover.
                if (Mentions(name, "financialyear", "fyear"))
                    return "2026-2027";

                if (Mentions(name, "quarter"))
                    return "Quarter 1";

                if (Mentions(name, "ifsc"))
                    return "ICIC0000001";

                if (Mentions(name, "gstin", "gst"))
                    return "29AAAAA0000A1Z5";

                if (Mentions(name, "pannumber", "pancard"))
                    return "AAAAA0000A";

                if (Mentions(name, "language"))
                    return "python";

                if (Mentions(name, "url", "uri", "link", "endpoint", "callback", "webhook"))
                    return "https://sweep.risebird.test/resource";

                if (Mentions(name, "email", "mailid", "recipient"))
                    return "sweep@risebird.test";

                if (Mentions(name, "json", "payload", "metadata"))
                    return "{}";

                if (Mentions(name, "phone", "mobile", "contactnumber"))
                    return "+919876543210";

                if (Mentions(name, "base64", "encoded", "thumbprint"))
                    return "dGVzdA==";

                if (Mentions(name, "guid", "correlation"))
                    return Guid.NewGuid().ToString();

                if (Mentions(name, "date", "timestamp"))
                    return "2026-01-15";

                if (Mentions(name, "year"))
                    return "2026";

                if (Mentions(name, "month"))
                    return "1";

                if (Mentions(name, "colour", "color"))
                    return "#336699";

                return "test";
            }

            private static bool Mentions(string haystack, params string[] needles) =>
                needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));

            private bool TryBuildTuple(Type type, int depth, string memberName, out object value)
            {
                value = null;
                if (!type.IsGenericType)
                    return false;

                var def = type.GetGenericTypeDefinition();
                var isTuple = def.FullName != null
                    && (def.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
                        || def.FullName.StartsWith("System.Tuple`", StringComparison.Ordinal));

                if (!isTuple)
                    return false;

                var args = type.GetGenericArguments()
                    .Select(a => Build(a, depth + 1, memberName))
                    .ToArray();

                try { value = Activator.CreateInstance(type, args); }
                catch { value = null; }
                return value != null;
            }

            private bool TryBuildCollection(Type type, int depth, string memberName, out object value)
            {
                value = null;

                if (type.IsArray)
                {
                    var element = type.GetElementType();
                    var array = Array.CreateInstance(element, CollectionSize);
                    for (var i = 0; i < CollectionSize; i++)
                        array.SetValue(Build(element, depth + 1, memberName), i);
                    value = array;
                    return true;
                }

                if (!type.IsGenericType || !typeof(IEnumerable).IsAssignableFrom(type))
                    return false;

                var args = type.GetGenericArguments();
                var def = type.GetGenericTypeDefinition();

                if (args.Length == 2
                    && (def == typeof(Dictionary<,>) || def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>)))
                {
                    var dictType = typeof(Dictionary<,>).MakeGenericType(args);
                    var dict = (IDictionary)Activator.CreateInstance(dictType);
                    for (var i = 0; i < CollectionSize; i++)
                    {
                        // Distinct keys matter: two entries that collided would silently make
                        // every generated dictionary single-entry.
                        var key = i == 0 ? Build(args[0], depth + 1, memberName) : SecondKey(args[0]);
                        if (key == null || dict.Contains(key))
                            continue;
                        dict[key] = Build(args[1], depth + 1, memberName);
                    }
                    value = dict;
                    return true;
                }

                if (args.Length != 1)
                    return false;

                var listType = typeof(List<>).MakeGenericType(args[0]);
                if (!type.IsAssignableFrom(listType))
                    return false;

                var list = (IList)Activator.CreateInstance(listType);
                for (var i = 0; i < CollectionSize; i++)
                    list.Add(Build(args[0], depth + 1, memberName));

                value = list;
                return true;
            }

            /// <summary>A second, distinct dictionary key so generated maps hold more than one entry.</summary>
            private static object SecondKey(Type keyType)
            {
                if (Nullable.GetUnderlyingType(keyType) is Type underlying)
                    keyType = underlying;

                if (keyType == typeof(string))
                    return "test-2";

                if (keyType == typeof(Guid))
                    return Guid.NewGuid();

                if (keyType.IsEnum)
                {
                    var values = Enum.GetValues(keyType);
                    return values.Length > 1 ? values.GetValue(1) : values.GetValue(0);
                }

                if (keyType.IsPrimitive)
                {
                    try { return Convert.ChangeType(2, keyType); }
                    catch { return null; }
                }

                return null;
            }

            private object BuildPoco(Type type, int depth)
            {
                // A type already being built further up the stack would recurse forever.
                if (depth > MaxDepth || _onPath.Contains(type))
                    return null;

                object instance;
                try
                {
                    instance = Construct(type, depth);
                }
                catch
                {
                    return null;
                }

                if (instance == null)
                    return null;

                _onPath.Add(type);
                try
                {
                    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (!property.CanWrite || property.GetIndexParameters().Length > 0)
                            continue;

                        // Past the depth budget, keep filling scalars — they are cheap and are
                        // what most branch conditions actually read — but stop following graphs.
                        if (depth >= MaxDepth && !IsScalar(property.PropertyType))
                            continue;

                        if (!IsScalar(property.PropertyType) && !ShouldRecurseInto(property.PropertyType))
                            continue;

                        try
                        {
                            var value = Build(property.PropertyType, depth + 1, property.Name);
                            if (value != null)
                                property.SetValue(instance, value);
                        }
                        catch
                        {
                            // A setter with validation in it is not worth failing the sweep over.
                        }
                    }
                }
                finally
                {
                    _onPath.Remove(type);
                }

                return instance;
            }

            private object Construct(Type type, int depth)
            {
                var constructors = type
                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(c => c.GetParameters().Length)
                    .ToList();

                if (constructors.Count == 0)
                    return type.IsValueType ? Activator.CreateInstance(type) : null;

                foreach (var constructor in constructors)
                {
                    try
                    {
                        var args = constructor.GetParameters()
                            .Select(p => Build(p.ParameterType, depth + 1, p.Name))
                            .ToArray();
                        return constructor.Invoke(args);
                    }
                    catch
                    {
                        // Try the next overload.
                    }
                }

                return null;
            }

            private static bool IsScalar(Type type)
            {
                if (Nullable.GetUnderlyingType(type) is Type underlying)
                    type = underlying;

                return type.IsPrimitive
                    || type.IsEnum
                    || type == typeof(string)
                    || type == typeof(decimal)
                    || type == typeof(DateTime)
                    || type == typeof(DateTimeOffset)
                    || type == typeof(TimeSpan)
                    || type == typeof(Guid);
            }

            /// <summary>
            /// Only Risebird's own types and generic containers are walked into. Following BCL or
            /// third-party graphs (Type, Assembly, HttpClient, …) reaches escape velocity fast and
            /// none of it is what the business layer branches on.
            /// </summary>
            private static bool ShouldRecurseInto(Type type)
            {
                if (type.IsArray)
                    return true;

                if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
                    return true;

                var name = type.Assembly.GetName().Name;
                return name != null && name.StartsWith("Risebird", StringComparison.Ordinal);
            }
        }
    }
}
