using Microsoft.Extensions.Logging;
using Moq;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.Fixtures
{
    /// <summary>
    /// One captured <c>ITransInterviewerAuditRepository.InsertAsync</c> call. Asserting against a
    /// recorded call is far more readable than an eleven-argument <c>Verify</c> expression, and it
    /// lets a test check the audit row's shape (metadata JSON, tag, source) without re-stating
    /// every other argument as <c>It.IsAny</c>.
    /// </summary>
    public sealed class CapturedAuditInsert
    {
        public int TransInterviewerId { get; init; }
        public string EventType { get; init; }
        public string FromValue { get; init; }
        public string ToValue { get; init; }
        public string ReasonCategory { get; init; }
        public int? TriggeringInterviewId { get; init; }
        public string ChangedBy { get; init; }
        public string AuditTag { get; init; }
        public string AuditSource { get; init; }
        public Guid? BackfillBatchId { get; init; }
        public string ExtraMetadata { get; init; }
    }

    /// <summary>One captured <c>UpdateIRFInterviewerActiveStatus</c> call.</summary>
    public sealed class CapturedActiveStatusUpdate
    {
        public int UserId { get; init; }
        public bool Status { get; init; }
        public int IrfId { get; init; }
        public string LoggedInUser { get; init; }
        public bool PreserveAvailability { get; init; }
        public bool? IsAvailable { get; init; }
    }

    /// <summary>One captured <c>RemoveOrShortlistInterviewerFromIRF</c> call.</summary>
    public sealed class CapturedIrfCascade
    {
        public int IrfId { get; init; }
        public string InterviewerEmail { get; init; }
        public string Action { get; init; }
    }

    /// <summary>One captured <c>UpdateVerifiedInterviewers</c> call.</summary>
    public sealed class CapturedVetUpdate
    {
        public IList<TransInterviewer> Interviewers { get; init; }
        public string LoggedInSme { get; init; }
    }

    /// <summary>One captured <c>UpdateAutoQcFieldsAsync</c> call.</summary>
    public sealed class CapturedAutoQcFieldUpdate
    {
        public int TransInterviewerId { get; init; }
        public string AutoQcVerdict { get; init; }
        public int TriggeringInterviewId { get; init; }
        public string FlagSource { get; init; }
    }

    /// <summary>
    /// Shared fixture for the two services that deactivate an interviewer on a skill channel —
    /// <see cref="DmIvChannelDeactivationService"/> (client-report "Remove Interviewer") and
    /// <see cref="DmIvAutoQcRemovalService"/> (auto-QC verdict, backfill and IRF-audit rails).
    ///
    /// Both drive the same three-step cascade — RB vet flag, IRF removal, IsActive/IsAvailable —
    /// followed by an audit insert, and both are ordered: a failing step must stop the ones after
    /// it. Every collaborator call is therefore recorded in order so a test can assert not just
    /// *that* something happened but that nothing happened after the failure.
    ///
    /// Defaults are the all-succeed happy path; <c>Given*Fails</c> flips one step at a time.
    /// </summary>
    public sealed class InterviewerLifecycleFixture
    {
        private TransInterviewerInterviewContext _contextForInterview;
        private TransInterviewerInterviewContext _contextForTransInterviewer;
        private int _futureAssignedCount;
        private bool _vetUpdateSucceeds = true;
        private bool _irfCascadeSucceeds = true;
        private bool _activeStatusSucceeds = true;
        private bool _duplicateRemovalAudit;

        public Mock<ITransInterviewerAuditRepository> Audit { get; } = new(MockBehavior.Loose);
        public Mock<IInterviewerService> Interviewer { get; } = new(MockBehavior.Loose);
        public Mock<IMeetingService> Meeting { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> ErrorLogger { get; } = new(MockBehavior.Loose);
        public Mock<ILogger<DmIvAutoQcRemovalService>> Log { get; } = new(MockBehavior.Loose);

        public List<CapturedAuditInsert> AuditInserts { get; } = new();
        public List<CapturedActiveStatusUpdate> ActiveStatusUpdates { get; } = new();
        public List<CapturedIrfCascade> IrfCascades { get; } = new();
        public List<CapturedVetUpdate> VetUpdates { get; } = new();
        public List<CapturedAutoQcFieldUpdate> AutoQcFieldUpdates { get; } = new();

        /// <summary>Collaborator calls in the order they happened, for ordering assertions.</summary>
        public List<string> CallOrder { get; } = new();

        public InterviewerLifecycleFixture()
        {
            _contextForInterview = Context();
            _contextForTransInterviewer = Context();
            WireAudit();
            WireInterviewer();
            WireMeeting();
        }

        // ── SUT factories ─────────────────────────────────────────────────────

        public DmIvChannelDeactivationService CreateDeactivationSut() =>
            new(Audit.Object, Meeting.Object, Interviewer.Object);

        public DmIvAutoQcRemovalService CreateRemovalSut() =>
            new(Audit.Object, Interviewer.Object, Meeting.Object, ErrorLogger.Object, Log.Object);

        // ── Arrange ───────────────────────────────────────────────────────────

        /// <summary>The context <c>GetContextForInterviewAsync</c> will return.</summary>
        public InterviewerLifecycleFixture GivenContext(TransInterviewerInterviewContext ctx)
        {
            _contextForInterview = ctx;
            return this;
        }

        /// <summary>No TransInterviewer row exists for the interview.</summary>
        public InterviewerLifecycleFixture GivenNoContext()
        {
            _contextForInterview = null;
            return this;
        }

        /// <summary>The context <c>GetContextByTransInterviewerIdAsync</c> will return.</summary>
        public InterviewerLifecycleFixture GivenContextByTransInterviewerId(TransInterviewerInterviewContext ctx)
        {
            _contextForTransInterviewer = ctx;
            return this;
        }

        public InterviewerLifecycleFixture GivenNoContextByTransInterviewerId()
        {
            _contextForTransInterviewer = null;
            return this;
        }

        public InterviewerLifecycleFixture GivenFutureAssignedCount(int count)
        {
            _futureAssignedCount = count;
            return this;
        }

        public InterviewerLifecycleFixture GivenVetUpdateFails()
        {
            _vetUpdateSucceeds = false;
            return this;
        }

        public InterviewerLifecycleFixture GivenIrfCascadeFails()
        {
            _irfCascadeSucceeds = false;
            return this;
        }

        public InterviewerLifecycleFixture GivenActiveStatusUpdateFails()
        {
            _activeStatusSucceeds = false;
            return this;
        }

        /// <summary>A forward-trigger removal audit already exists for this interview.</summary>
        public InterviewerLifecycleFixture GivenDuplicateRemovalAudit()
        {
            _duplicateRemovalAudit = true;
            return this;
        }

        // ── Assert helpers ────────────────────────────────────────────────────

        /// <summary>The single audit row written, failing with a clear message when there is not exactly one.</summary>
        public CapturedAuditInsert TheAuditRow
        {
            get
            {
                if (AuditInserts.Count != 1)
                    throw new InvalidOperationException(
                        $"Expected exactly one audit insert, saw {AuditInserts.Count}: " +
                        string.Join(", ", AuditInserts.Select(a => a.EventType)));
                return AuditInserts[0];
            }
        }

        public CapturedIrfCascade TheIrfCascade
        {
            get
            {
                if (IrfCascades.Count != 1)
                    throw new InvalidOperationException($"Expected exactly one IRF cascade, saw {IrfCascades.Count}.");
                return IrfCascades[0];
            }
        }

        public CapturedActiveStatusUpdate TheActiveStatusUpdate
        {
            get
            {
                if (ActiveStatusUpdates.Count != 1)
                    throw new InvalidOperationException(
                        $"Expected exactly one active-status update, saw {ActiveStatusUpdates.Count}.");
                return ActiveStatusUpdates[0];
            }
        }

        public CapturedVetUpdate TheVetUpdate
        {
            get
            {
                if (VetUpdates.Count != 1)
                    throw new InvalidOperationException($"Expected exactly one vet update, saw {VetUpdates.Count}.");
                return VetUpdates[0];
            }
        }

        /// <summary>Nothing at all was written — used for every early-return branch.</summary>
        public bool NothingWasWritten =>
            AuditInserts.Count == 0
            && ActiveStatusUpdates.Count == 0
            && IrfCascades.Count == 0
            && VetUpdates.Count == 0
            && AutoQcFieldUpdates.Count == 0;

        // ── Builders ──────────────────────────────────────────────────────────

        public static TransInterviewerInterviewContext Context(
            int transInterviewerId = 100,
            int userId = 9,
            int irfId = 3,
            bool isActive = true,
            string interviewerEmail = "iv@risebird.io",
            bool isRbVetYes = true,
            bool rbVetAlreadyNo = false,
            string clientAction = null,
            string autoQcVerdict = null,
            string interviewerCodedId = "AdLo1234") => new()
            {
                TransInterviewerId = transInterviewerId,
                UserId = userId,
                IrfId = irfId,
                IsActive = isActive,
                InterviewerEmail = interviewerEmail,
                InterviewerCodedId = interviewerCodedId,
                IsRbVetYes = isRbVetYes,
                RbVetAlreadyNo = rbVetAlreadyNo,
                ClientAction = clientAction,
                AutoQcVerdict = autoQcVerdict
            };

        // ── Moq wiring ────────────────────────────────────────────────────────

        private void WireAudit()
        {
            Audit.Setup(x => x.GetContextForInterviewAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(() => _contextForInterview);

            Audit.Setup(x => x.GetContextByTransInterviewerIdAsync(It.IsAny<int>()))
                .ReturnsAsync(() => _contextForTransInterviewer);

            Audit.Setup(x => x.GetFutureAssignedCountForInterviewAsync(It.IsAny<int>()))
                .ReturnsAsync(() => _futureAssignedCount);

            Audit.Setup(x => x.HasAutoQcRemovalAuditForInterviewAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(() => _duplicateRemovalAudit);

            Audit.Setup(x => x.HasAutoQcFlagAuditForInterviewAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            Audit.Setup(x => x.InsertAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .Callback<int, string, string, string, string, int?, string, string, string, Guid?, string>(
                    (transId, eventType, from, to, reason, triggerId, changedBy, tag, source, batchId, metadata) =>
                    {
                        AuditInserts.Add(new CapturedAuditInsert
                        {
                            TransInterviewerId = transId,
                            EventType = eventType,
                            FromValue = from,
                            ToValue = to,
                            ReasonCategory = reason,
                            TriggeringInterviewId = triggerId,
                            ChangedBy = changedBy,
                            AuditTag = tag,
                            AuditSource = source,
                            BackfillBatchId = batchId,
                            ExtraMetadata = metadata
                        });
                        CallOrder.Add("InsertAsync:" + eventType);
                    })
                .Returns(Task.CompletedTask);

            Audit.Setup(x => x.UpdateAutoQcFieldsAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Callback<int, string, int, string>((transId, verdict, triggerId, flagSource) =>
                {
                    AutoQcFieldUpdates.Add(new CapturedAutoQcFieldUpdate
                    {
                        TransInterviewerId = transId,
                        AutoQcVerdict = verdict,
                        TriggeringInterviewId = triggerId,
                        FlagSource = flagSource
                    });
                    CallOrder.Add("UpdateAutoQcFieldsAsync");
                })
                .Returns(Task.CompletedTask);
        }

        private void WireInterviewer()
        {
            Interviewer.Setup(x => x.UpdateVerifiedInterviewers(It.IsAny<IList<TransInterviewer>>(), It.IsAny<string>()))
                .Callback<IList<TransInterviewer>, string>((interviewers, sme) =>
                {
                    VetUpdates.Add(new CapturedVetUpdate { Interviewers = interviewers, LoggedInSme = sme });
                    CallOrder.Add("UpdateVerifiedInterviewers");
                })
                .ReturnsAsync(() => _vetUpdateSucceeds);

            Interviewer.Setup(x => x.UpdateIRFInterviewerActiveStatus(
                    It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool?>()))
                .Callback<int, bool, int, string, bool, bool?>(
                    (userId, status, irfId, user, preserve, available) =>
                    {
                        ActiveStatusUpdates.Add(new CapturedActiveStatusUpdate
                        {
                            UserId = userId,
                            Status = status,
                            IrfId = irfId,
                            LoggedInUser = user,
                            PreserveAvailability = preserve,
                            IsAvailable = available
                        });
                        CallOrder.Add("UpdateIRFInterviewerActiveStatus");
                    })
                .ReturnsAsync(() => _activeStatusSucceeds);
        }

        private void WireMeeting()
        {
            Meeting.Setup(x => x.RemoveOrShortlistInterviewerFromIRF(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<int, string, string>((irfId, email, action) =>
                {
                    IrfCascades.Add(new CapturedIrfCascade { IrfId = irfId, InterviewerEmail = email, Action = action });
                    CallOrder.Add("RemoveOrShortlistInterviewerFromIRF");
                })
                .ReturnsAsync(() => _irfCascadeSucceeds);
        }
    }
}
