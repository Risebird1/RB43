using Moq;

namespace Risebird.Business.Tests.Infrastructure
{
    /// <summary>
    /// Hands every un-stubbed mock member a populated result: a repository returns two rows
    /// rather than none, a lookup returns an object rather than null, a flag reads true.
    ///
    /// This is the lever that walks the happy path. Moq's own
    /// <see cref="DefaultValueProvider.Mock"/> returns empty collections and default scalars, so
    /// a service short-circuits on its first "if (rows.Count == 0) return" and the mapping,
    /// aggregation and formatting below it never executes.
    /// </summary>
    internal sealed class PopulatedValueProvider : DefaultValueProvider
    {
        private readonly PopulationProfile _profile;

        public PopulatedValueProvider(PopulationProfile profile = null) =>
            _profile = profile ?? PopulationProfile.Affirmative;

        protected override object GetDefaultValue(Type type, Mock mock)
        {
            var value = PopulatedValueFactory.Create(type, _profile);
            if (value != null)
                return value;

            // Collaborator interfaces still need to be mocks, and the nested mock inherits this
            // provider so the graph stays populated all the way down.
            if (type.IsInterface || type.IsAbstract)
            {
                try
                {
                    var nested = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type));
                    nested.DefaultValueProvider = this;
                    return nested.Object;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Makes every un-stubbed mock member fail. Throwing out of the value provider surfaces at
    /// the call site inside the service, so it reaches both <c>await _repo.Foo()</c> and plain
    /// synchronous calls without needing a Setup per member.
    ///
    /// Nearly every business method wraps its body in try/catch, logs and returns a fallback.
    /// Those catch arms are three or four lines each across hundreds of methods and are a large
    /// share of the uncovered business layer; nothing but a failing collaborator reaches them.
    /// </summary>
    internal sealed class FaultingValueProvider : DefaultValueProvider
    {
        internal const string FailureMessage = "Injected collaborator failure (coverage sweep).";

        protected override object GetDefaultValue(Type type, Mock mock) =>
            throw new InvalidOperationException(FailureMessage);
    }
}
