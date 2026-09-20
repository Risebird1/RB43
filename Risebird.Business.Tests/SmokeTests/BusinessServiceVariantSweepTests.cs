using Moq;
using Risebird.Business.Tests.Infrastructure;

namespace Risebird.Business.Tests.SmokeTests
{
    /// <summary>
    /// Drives every public method of every business service a second and third time, with the
    /// collaborators behaving differently each pass.
    ///
    /// <see cref="BusinessServiceMethodSweepTests"/> already drives them with empty input, which
    /// exercises the guard clauses and early returns. That leaves two large regions of the
    /// business layer untouched: the work a method does when a repository actually returns rows,
    /// and the catch arm it runs when a collaborator fails. These sweeps cover both.
    ///
    /// What they assert is deliberately narrow — that no method hangs, and that a failing
    /// collaborator does not take the process down. They are coverage vehicles, not behavioural
    /// specifications: the assertion that a given method returns the right answer for a given
    /// input belongs in that service's own hand-written suite, where the expected value can be
    /// stated. Read a green run here as "nothing exploded", not "the logic is correct".
    /// </summary>
    public class BusinessServicePopulatedSweepTests
    {
        [Theory]
        [MemberData(nameof(ServiceAndProfile))]
        public async Task No_public_method_hangs_when_collaborators_return_data(
            string serviceTypeName, string profileName)
        {
            var serviceType = BusinessServiceCatalog.ResolveServiceType(serviceTypeName);
            var profile = PopulationProfile.All().Single(p => p.Name == profileName);

            var mocker = new AutoMocker(
                valueProvider: new PopulatedValueProvider(profile),
                httpStatus: profile.HttpStatus);

            object service;
            try
            {
                service = mocker.CreateService(serviceType);
            }
            catch
            {
                // A constructor that rejects this profile's data (a blank URL, say) is the
                // baseline sweep's territory — it builds every service with harmless defaults.
                return;
            }

            var hung = new List<string>();
            foreach (var method in BusinessServiceCatalog.MethodsOf(serviceType))
            {
                var key = BusinessServiceCatalog.KeyFor(serviceType, method);
                var outcome = await ServiceMethodInvoker.TryInvokeAsync(
                    service, method, mocker, key,
                    (type, name) => PopulatedValueFactory.Create(type, profile, name));

                if (outcome.TimedOut)
                    hung.Add(key);
            }

            Assert.True(hung.Count == 0,
                $"[{profileName}] did not finish within "
                + $"{ServiceMethodInvoker.InvocationBudget.TotalSeconds:0}s: "
                + string.Join(", ", hung));
        }

        public static IEnumerable<object[]> ServiceAndProfile() =>
            from service in BusinessServiceCatalog.ServiceTypeNames()
            from profile in PopulationProfile.All()
            select new[] { service[0], profile.Name };
    }

    public class BusinessServiceFailingCollaboratorSweepTests
    {
        [Theory]
        [MemberData(nameof(AllServiceNames))]
        public async Task No_public_method_hangs_when_every_collaborator_throws(string serviceTypeName)
        {
            var serviceType = BusinessServiceCatalog.ResolveServiceType(serviceTypeName);
            var mocker = new AutoMocker(valueProvider: new FaultingValueProvider());

            object service;
            try
            {
                service = mocker.CreateService(serviceType);
            }
            catch
            {
                // A constructor that reads from a collaborator cannot be built in this mode.
                // The baseline sweep already covers construction, so there is nothing to add.
                return;
            }

            var hung = new List<string>();
            foreach (var method in BusinessServiceCatalog.MethodsOf(serviceType))
            {
                var key = BusinessServiceCatalog.KeyFor(serviceType, method);
                var outcome = await ServiceMethodInvoker.TryInvokeAsync(service, method, mocker, key);

                if (outcome.TimedOut)
                    hung.Add(key);
            }

            Assert.True(hung.Count == 0,
                $"did not finish within {ServiceMethodInvoker.InvocationBudget.TotalSeconds:0}s: "
                + string.Join(", ", hung));
        }

        public static IEnumerable<object[]> AllServiceNames() => BusinessServiceCatalog.ServiceTypeNames();
    }

    /// <summary>
    /// Guards the sweep machinery itself. A value provider that silently stopped populating
    /// anything would leave every sweep above green while covering nothing, so the failure mode
    /// worth catching is the tooling going quiet — not the services misbehaving.
    /// </summary>
    public class SweepValueProviderTests
    {
        [Fact]
        public void The_populated_factory_fills_collections_rather_than_leaving_them_empty()
        {
            var list = (List<string>)PopulatedValueFactory.Create(typeof(List<string>));

            Assert.NotEmpty(list);
            Assert.All(list, item => Assert.False(string.IsNullOrEmpty(item)));
        }

        [Fact]
        public void The_populated_factory_prefers_non_default_scalars()
        {
            Assert.Equal(true, PopulatedValueFactory.Create(typeof(bool)));
            Assert.NotEqual(0, (int)PopulatedValueFactory.Create(typeof(int)));
            Assert.NotEqual(default, (DateTime)PopulatedValueFactory.Create(typeof(DateTime)));
        }

        [Fact]
        public void The_populated_factory_terminates_on_a_self_referencing_type()
        {
            // The graph walk is depth- and cycle-bounded; without that this never returns.
            var node = PopulatedValueFactory.Create(typeof(SelfReferencing));

            Assert.NotNull(node);
        }

        [Fact]
        public void A_populated_mock_hands_back_a_non_empty_collection()
        {
            var mock = new Mock<ISampleCollaborator> { DefaultValueProvider = new PopulatedValueProvider() };

            Assert.NotEmpty(mock.Object.GetRows());
        }

        [Fact]
        public async Task A_faulting_mock_throws_from_an_awaited_member()
        {
            var mock = new Mock<ISampleCollaborator> { DefaultValueProvider = new FaultingValueProvider() };

            await Assert.ThrowsAsync<InvalidOperationException>(() => mock.Object.LoadAsync());
        }

        public interface ISampleCollaborator
        {
            List<string> GetRows();
            Task<string> LoadAsync();
        }

        public class SelfReferencing
        {
            public string Name { get; set; }
            public SelfReferencing Parent { get; set; }
            public List<SelfReferencing> Children { get; set; }
        }
    }
}
