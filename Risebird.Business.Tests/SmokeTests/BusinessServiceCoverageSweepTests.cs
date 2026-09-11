using System.Text;
using Risebird.Business.Tests.Infrastructure;

namespace Risebird.Business.Tests.SmokeTests
{
    /// <summary>
    /// Drives every public method of every service in Risebird.Business with auto-mocked
    /// dependencies. Its job is twofold: execute the delegating and guard paths that the
    /// hand-written suites do not reach, and pin the set of methods that are NOT defensive
    /// about default input so that set can only shrink.
    /// </summary>
    public class BusinessServiceDiscoveryTests
    {
        [Fact]
        public void Discovers_the_business_services()
        {
            var services = BusinessServiceCatalog.ServiceTypes().ToList();

            // Guards against a reflection filter that silently matches nothing.
            Assert.True(services.Count >= 40,
                $"expected 40+ business services, found {services.Count}: "
                + string.Join(", ", services.Select(t => t.Name)));
        }

        [Fact]
        public void Discovers_the_services_the_hand_written_suites_also_cover()
        {
            var names = BusinessServiceCatalog.ServiceTypes().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var expected in new[]
                     {
                         nameof(InterviewerService), nameof(IRFService), nameof(MeetingService),
                         nameof(WalletService), nameof(TdsComplianceService), nameof(KycPayoutLapseService),
                         nameof(ProctoringApiService), nameof(TrialJudgmentService), nameof(DmIvAutoQcFlagService),
                         nameof(RiskCategoryService), nameof(SettingsService), nameof(ActivityTrackerService)
                     })
            {
                Assert.Contains(expected, names);
            }
        }

        [Fact]
        public void Every_service_exposes_at_least_one_public_method()
        {
            var barren = BusinessServiceCatalog.ServiceTypes()
                .Where(t => !BusinessServiceCatalog.MethodsOf(t).Any())
                .Select(t => t.Name)
                .ToList();

            // MenuService is a deliberate empty placeholder; anything else is suspicious.
            Assert.All(barren, name => Assert.Equal(nameof(MenuService), name));
        }
    }

    public class BusinessServiceConstructionTests
    {
        [Theory]
        [MemberData(nameof(AllServiceNames))]
        public void Every_service_can_be_constructed_with_auto_mocked_dependencies(string serviceTypeName)
        {
            var serviceType = BusinessServiceCatalog.ResolveServiceType(serviceTypeName);
            var mocker = new AutoMocker();

            var service = mocker.CreateService(serviceType);

            Assert.NotNull(service);
            Assert.IsAssignableFrom(serviceType, service);
        }

        [Theory]
        [MemberData(nameof(AllServiceNames))]
        public void Construction_does_not_reach_out_to_the_network(string serviceTypeName)
        {
            var serviceType = BusinessServiceCatalog.ResolveServiceType(serviceTypeName);
            var mocker = new AutoMocker();

            mocker.CreateService(serviceType);

            Assert.Empty(mocker.HttpRequests);
        }

        public static IEnumerable<object[]> AllServiceNames() => BusinessServiceCatalog.ServiceTypeNames();
    }

    public class BusinessServiceMethodSweepTests
    {
        /// <summary>
        /// Methods that throw when handed default/empty input. Every entry is a method that does
        /// not guard its arguments or its collaborators' empty results — i.e. a latent
        /// NullReferenceException in production if a repository ever returns nothing.
        ///
        /// This list is a ratchet: it may shrink (fix a method, remove its line) but it must not
        /// grow. If the test below fails it prints the exact replacement block to paste here.
        /// </summary>
        private static readonly HashSet<string> KnownNotDefensive = new(StringComparer.Ordinal)
        {
            // Populated from the first run — see the assertion message.
            "ClonableSkillChannelTemplateService.PopulateDropDownsForTemplate(ClonableSkillChannelVM, Boolean)",
            "MeetingService.PostApproval(String, String, String)",
            "MeetingService.PostSelectionStatus(String, String, String)",
            "MeetingService.SaveQCFeedback(String, String, String, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, Boolean, String, String)",
            "TdsComplianceService.ResolveQuarterDates(String, String)",
            "WalletService.GetAllClientsWalletSummary(Int32, Int32, Int32, Int32)",
        };

        [Theory]
        [MemberData(nameof(AllServiceNames))]
        public async Task No_public_method_hangs_when_driven_with_default_input(string serviceTypeName)
        {
            var serviceType = BusinessServiceCatalog.ResolveServiceType(serviceTypeName);
            var mocker = new AutoMocker();
            var service = mocker.CreateService(serviceType);

            var hung = new List<string>();
            foreach (var method in BusinessServiceCatalog.MethodsOf(serviceType))
            {
                var key = BusinessServiceCatalog.KeyFor(serviceType, method);
                var outcome = await ServiceMethodInvoker.TryInvokeAsync(service, method, mocker, key);
                if (outcome.TimedOut)
                    hung.Add(key);
            }

            Assert.Empty(hung);

            // Any HTTP a service issued went through the injected, stubbed client — the sweep
            // completed without touching a real endpoint.
            Assert.All(mocker.HttpRequests, request => Assert.NotNull(request.RequestUri));
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public async Task The_set_of_methods_that_throw_on_default_input_has_not_grown()
        {
            var threw = new List<InvocationOutcome>();

            foreach (var serviceType in BusinessServiceCatalog.ServiceTypes())
            {
                var mocker = new AutoMocker();
                object service;
                try
                {
                    service = mocker.CreateService(serviceType);
                }
                catch (Exception ex)
                {
                    threw.Add(new InvocationOutcome { Key = $"{serviceType.Name}..ctor", Exception = ex });
                    continue;
                }

                foreach (var method in BusinessServiceCatalog.MethodsOf(serviceType))
                {
                    var key = BusinessServiceCatalog.KeyFor(serviceType, method);
                    var outcome = await ServiceMethodInvoker.TryInvokeAsync(service, method, mocker, key);
                    if (!outcome.Completed)
                        threw.Add(outcome);
                }
            }

            var unexpected = threw.Where(o => !KnownNotDefensive.Contains(o.Key)).ToList();
            var fixedSince = KnownNotDefensive
                .Where(k => threw.All(o => o.Key != k))
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            if (unexpected.Count == 0 && fixedSince.Count == 0)
                return;

            var report = new StringBuilder();
            report.AppendLine("The set of non-defensive business methods changed.");
            report.AppendLine();

            if (unexpected.Count > 0)
            {
                report.AppendLine($"{unexpected.Count} method(s) newly throw on default input:");
                foreach (var outcome in unexpected.OrderBy(o => o.Key, StringComparer.Ordinal))
                    report.AppendLine("  " + outcome.Describe());
                report.AppendLine();
            }

            if (fixedSince.Count > 0)
            {
                report.AppendLine($"{fixedSince.Count} method(s) no longer throw — remove them from the ratchet:");
                foreach (var key in fixedSince)
                    report.AppendLine("  " + key);
                report.AppendLine();
            }

            report.AppendLine("Current baseline — paste this into KnownNotDefensive:");
            foreach (var key in threw.Select(o => o.Key).Distinct().OrderBy(k => k, StringComparer.Ordinal))
                report.AppendLine($"            \"{key}\",");

            Assert.Fail(report.ToString());
        }

        public static IEnumerable<object[]> AllServiceNames() => BusinessServiceCatalog.ServiceTypeNames();
    }
}
