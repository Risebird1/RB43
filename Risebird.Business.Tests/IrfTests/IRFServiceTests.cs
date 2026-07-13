using Moq;
using Risebird.Business;
using Risebird.Business.Tests.Fixtures;
using Risebird.Business.Tests.Infrastructure;
using Risebird.Entities.ViewModel;
using IrfEntity = Risebird.Entities.ViewModel.IRF;

namespace Risebird.Business.Tests.IrfTests
{
    public class IRFServiceMethodCoverageTests
    {
        [Fact]
        public void Declared_public_methods_are_discoverable()
        {
            var methods = ServiceMethodInvoker.GetPublicInstanceMethods(typeof(IRFService)).ToList();
            Assert.NotEmpty(methods);
            Assert.Contains(methods, m => m.Name == nameof(IRFService.AddIRFDetails));
            Assert.Contains(methods, m => m.Name == nameof(IRFService.UpdateIRF));
            Assert.Contains(methods, m => m.Name == nameof(IRFService.GetAllIRFs));
        }

        [Theory]
        [MemberData(nameof(AllPublicMethods))]
        public async Task Every_public_method_can_be_invoked_without_unhandled_exception(string methodKey)
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();
            var method = ServiceMethodInvoker.GetPublicInstanceMethods(typeof(IRFService))
                .Single(m => ServiceMethodInvoker.GetMethodKey(m) == methodKey);

            var exception = await Record.ExceptionAsync(() => ServiceMethodInvoker.InvokeAsync(sut, method));
            Assert.Null(exception);
        }

        public static IEnumerable<object[]> AllPublicMethods() =>
            ServiceMethodInvoker.GetPublicInstanceMethods(typeof(IRFService))
                .Select(m => new object[] { ServiceMethodInvoker.GetMethodKey(m) });
    }

    public class IRFServiceIndexSyncTests
    {
        [Fact]
        public async Task AddIRFDetails_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();
            var model = new IRFVm
            {
                IRFId = "SC-NEW",
                JDTiltle = "Java | Dev",
                ClientId = 1,
                RisebirdHr = 2
            };

            var result = await sut.AddIRFDetails(model, "admin@test.com", "https://localhost");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(101), Times.Once);
        }

        [Fact]
        public async Task UpdateIRF_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();
            var model = new IRFVm
            {
                id = 77,
                IRFId = "SC-77",
                JDTiltle = "C# | Dev",
                ClientId = 1,
                RisebirdHr = 2,
                ResourceUrl = "/IRF/EditIRF?id=77"
            };

            var result = await sut.UpdateIRF(model, "admin@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(77), Times.Once);
        }

        [Fact]
        public async Task ArchiveIRF_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.ArchiveIRF(15);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(15), Times.Once);
        }

        [Fact]
        public async Task UnArchiveIRF_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UnArchiveIRF(16);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(16), Times.Once);
        }

        [Fact]
        public void PlayIRF_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();

            var result = sut.PlayIRF(18, "SC-18");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(18), Times.Once);
        }

        [Fact]
        public async Task PauseIRF_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.PauseIRF(19, "SC-19", "admin@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(19), Times.Once);
        }

        [Fact]
        public async Task CreateSkillChannelFromTemplate_upserts_by_name()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();
            var model = new IRFVm
            {
                IRFId = "SC-TMPL",
                JDTiltle = "Python | Dev",
                ClientId = 1,
                TemplateName = "T1",
                AssessmentGroupId = "1",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1)
            };

            var result = await sut.CreateSkillChannelFromTemplate(model, "admin@test.com", "HR");

            Assert.True(result.Item1);
            fixture.Repository.Verify(x => x.GetSkillChannelDetailsByName("SC-TMPL"), Times.AtLeastOnce);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(42), Times.Once);
        }

        [Fact]
        public async Task EditSkillChannelCreatedFromTemplate_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();
            var model = new IRFVm { id = 33, IRFId = "SC-33", JDTiltle = "Go | Dev" };

            var result = await sut.EditSkillChannelCreatedFromTemplate(model, "admin@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(33), Times.Once);
        }

        [Fact]
        public async Task UpdateSkillChannelEmbeddableUrl_upserts_by_name()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateSkillChannelEmbeddableUrl("SC-AF", "https://affinda/embed");

            Assert.True(result);
            fixture.Repository.Verify(x => x.GetSkillChannelDetailsByName("SC-AF"), Times.Once);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(42), Times.Once);
        }

        [Fact]
        public async Task UpdateNewPriority_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateNewPriority(44, 2);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(44), Times.Once);
        }

        [Fact]
        public async Task UpdateIRFInterviewerBillingCycle_upserts_search_index()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateIRFInterviewerBillingCycle(45, 1, "admin@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(45), Times.Once);
        }

        [Fact]
        public async Task GetAllIRFs_uses_search_when_enabled()
        {
            var fixture = new IRFServiceFixture();
            var sut = fixture.CreateSut();
            fixture.ListingSearch.Setup(x => x.SearchAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<IRFFilter>()))
                .ReturnsAsync(new List<IrfEntity> { new() { id = 1, IRFId = "SC-1", TotalBudget = 100, TotalBudgetConsumed = 10 } });

            var result = (await sut.GetAllIRFs("admin@test.com", "Admin", 1))?.ToList();

            Assert.NotNull(result);
            Assert.Single(result!);
            fixture.ListingSearch.Verify(x => x.SearchAsync("admin@test.com", "Admin", 1, null, null), Times.Once);
            fixture.Repository.Verify(x => x.GetAllIRFs(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<IRFFilter>()), Times.Never);
        }

        [Fact]
        public async Task AddIRFDetails_skips_upsert_when_search_disabled()
        {
            var fixture = new IRFServiceFixture();
            fixture.ListingSearch.SetupGet(x => x.IsEnabled).Returns(false);
            var sut = fixture.CreateSut();
            // CreateSut sets IsEnabled=true; force disabled after construction.
            fixture.ListingSearch.SetupGet(x => x.IsEnabled).Returns(false);
            var model = new IRFVm { IRFId = "SC-X", JDTiltle = "Java | Dev", ClientId = 1 };

            var result = await sut.AddIRFDetails(model, "admin@test.com", "https://localhost");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertIrfAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
