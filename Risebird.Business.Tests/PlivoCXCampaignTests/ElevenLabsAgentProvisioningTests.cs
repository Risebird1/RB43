using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Risebird.Business.PlivoCX;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.ElevenLabsModels;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.PlivoCXCampaignTests
{
    /// <summary>
    /// Dashboard/CreateElevenLabsCampaignAgents (PlivoCXCampaignService.ProvisionElevenLabsAgentsAsync).
    /// It must never create a second ElevenLabs agent for a purpose it already has one for - also
    /// when an earlier run created the agent but could not save its PlivoCXAgent row - and must
    /// report, not hide, a row that could not be saved.
    /// </summary>
    public class ElevenLabsAgentProvisioningTests
    {
        private readonly Mock<IPlivoCXCampaignRepository> _repo = new();
        private readonly Mock<IElevenLabsAgentService> _elevenLabs = new();
        private readonly List<PlivoCXAgent> _rows = new();
        private int _nextId = 100;

        public ElevenLabsAgentProvisioningTests()
        {
            _repo.Setup(r => r.GetAgentsAsync(false)).ReturnsAsync(() => _rows.ToList());
            _repo.Setup(r => r.InsertAgentAsync(It.IsAny<PlivoCXAgent>())).ReturnsAsync(() => _nextId++);
            _elevenLabs.Setup(e => e.CreateCampaignAgentAsync(It.IsAny<ElevenLabsCreateAgentRequest>()))
                .ReturnsAsync((ElevenLabsCreateAgentRequest r) => new ElevenLabsCreateAgentResponse { Success = true, AgentId = "new_" + r.Name });
            _elevenLabs.Setup(e => e.UpdateCampaignAgentAsync(It.IsAny<string>(), It.IsAny<ElevenLabsCreateAgentRequest>()))
                .ReturnsAsync((string id, ElevenLabsCreateAgentRequest _) => new ElevenLabsCreateAgentResponse { Success = true, AgentId = id });
            _elevenLabs.Setup(e => e.FindCampaignAgentIdByNameAsync(It.IsAny<string>())).ReturnsAsync((string)null);
        }

        private PlivoCXCampaignService Service() => new PlivoCXCampaignService(
            new ConfigurationBuilder().Build(),
            Mock.Of<IPlivoCXService>(),
            Mock.Of<IPlivoCXCallRepository>(),
            _repo.Object,
            Mock.Of<IPhoneNumberHelper>(),
            Mock.Of<IApplicationErrorLogger>(),
            _elevenLabs.Object);

        private static string NameOf(string purpose) =>
            ElevenLabsCampaignAgents.Definitions.Single(d => d.Purpose == purpose).ElevenLabsName;

        [Fact]
        public async Task A_first_run_creates_the_three_agents_and_saves_a_row_for_each()
        {
            var results = await Service().ProvisionElevenLabsAgentsAsync();

            Assert.All(results, r => { Assert.Null(r.Error); Assert.Equal("created", r.Action); Assert.True(r.PlivoCXAgentId > 0); });
            _elevenLabs.Verify(e => e.CreateCampaignAgentAsync(It.IsAny<ElevenLabsCreateAgentRequest>()), Times.Exactly(3));
            _repo.Verify(r => r.InsertAgentAsync(It.Is<PlivoCXAgent>(a =>
                a.Provider == PlivoCXCampaignProvider.ElevenLabs && a.ProviderAgentId.StartsWith("new_") && a.TriggerUrl == "")), Times.Exactly(3));
        }

        [Fact]
        public async Task Agents_created_by_a_run_whose_rows_were_not_saved_are_found_by_name_and_updated_not_duplicated()
        {
            // The 28 Oct run: ElevenLabs agents exist, PlivoCXAgent has no ElevenLabs rows.
            _elevenLabs.Setup(e => e.FindCampaignAgentIdByNameAsync(NameOf("schedule"))).ReturnsAsync("agent_sched");
            _elevenLabs.Setup(e => e.FindCampaignAgentIdByNameAsync(NameOf("reschedule"))).ReturnsAsync("agent_resched");
            _elevenLabs.Setup(e => e.FindCampaignAgentIdByNameAsync(NameOf("confirm"))).ReturnsAsync("agent_confirm");

            var results = await Service().ProvisionElevenLabsAgentsAsync();

            _elevenLabs.Verify(e => e.CreateCampaignAgentAsync(It.IsAny<ElevenLabsCreateAgentRequest>()), Times.Never);
            _elevenLabs.Verify(e => e.UpdateCampaignAgentAsync("agent_sched", It.IsAny<ElevenLabsCreateAgentRequest>()), Times.Once);
            _repo.Verify(r => r.InsertAgentAsync(It.Is<PlivoCXAgent>(a => a.Purpose == "confirm" && a.ProviderAgentId == "agent_confirm")), Times.Once);
            Assert.All(results, r => { Assert.Null(r.Error); Assert.Equal("updated", r.Action); });
        }

        [Fact]
        public async Task A_saved_row_is_used_directly_and_updated_in_place()
        {
            foreach (var d in ElevenLabsCampaignAgents.Definitions)
                _rows.Add(new PlivoCXAgent { Id = 7, Purpose = d.Purpose, Provider = "ElevenLabs", ProviderAgentId = "saved_" + d.Purpose });

            var results = await Service().ProvisionElevenLabsAgentsAsync();

            _elevenLabs.Verify(e => e.FindCampaignAgentIdByNameAsync(It.IsAny<string>()), Times.Never);
            _elevenLabs.Verify(e => e.CreateCampaignAgentAsync(It.IsAny<ElevenLabsCreateAgentRequest>()), Times.Never);
            _elevenLabs.Verify(e => e.UpdateCampaignAgentAsync("saved_schedule", It.IsAny<ElevenLabsCreateAgentRequest>()), Times.Once);
            _repo.Verify(r => r.InsertAgentAsync(It.IsAny<PlivoCXAgent>()), Times.Never);
            _repo.Verify(r => r.UpdateAgentAsync(It.IsAny<PlivoCXAgent>()), Times.Exactly(3));
            Assert.All(results, r => Assert.Equal("updated", r.Action));
        }

        [Fact]
        public async Task Plivo_rows_with_the_same_purpose_are_not_mistaken_for_ElevenLabs_ones()
        {
            _rows.Add(new PlivoCXAgent { Id = 1, Purpose = "schedule", Provider = "Plivo", TriggerUrl = "https://agentflow.plivo.com/x" });
            _rows.Add(new PlivoCXAgent { Id = 2, Purpose = "schedule", Provider = null });   // a row from before the Provider column

            await Service().ProvisionElevenLabsAgentsAsync();

            _repo.Verify(r => r.UpdateAgentAsync(It.IsAny<PlivoCXAgent>()), Times.Never);
            _repo.Verify(r => r.InsertAgentAsync(It.Is<PlivoCXAgent>(a => a.Purpose == "schedule" && a.Provider == "ElevenLabs")), Times.Once);
        }

        [Fact]
        public async Task A_row_that_cannot_be_saved_is_reported_with_the_fix()
        {
            // InsertAgentAsync returns 0 when the insert fails, e.g. the Provider columns are missing.
            _repo.Setup(r => r.InsertAgentAsync(It.IsAny<PlivoCXAgent>())).ReturnsAsync(0);

            var results = await Service().ProvisionElevenLabsAgentsAsync();

            Assert.All(results, r =>
            {
                Assert.NotNull(r.Error);
                Assert.Contains(r.AgentId, r.Error);
                Assert.Contains("PlivoCXAgent.sql", r.Error);
                Assert.Null(r.Action);
            });
        }

        [Fact]
        public async Task An_ElevenLabs_failure_is_reported_and_nothing_is_saved()
        {
            _elevenLabs.Setup(e => e.CreateCampaignAgentAsync(It.IsAny<ElevenLabsCreateAgentRequest>()))
                .ReturnsAsync(new ElevenLabsCreateAgentResponse { Success = false, Error = "HTTP 401: invalid_api_key" });

            var results = await Service().ProvisionElevenLabsAgentsAsync();

            Assert.All(results, r => Assert.Equal("HTTP 401: invalid_api_key", r.Error));
            _repo.Verify(r => r.InsertAgentAsync(It.IsAny<PlivoCXAgent>()), Times.Never);
        }
    }
}
