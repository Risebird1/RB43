using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IMeetingService
    {
        Task<List<MeetingVM>> GetMeetingList();
        Task<bool> AddMeeting(AddUpdateMeetingVM meeting);
        Task<List<MeetingVM>> GetAllMeetingList(DateTime startDate, DateTime endDate, string emailId = null);
        Task<List<MeetingVM>> GetAllCancelInterviewList(DateTime startDate, DateTime endDate, string emailId = null);
        Task<bool> UpdateMeeting(AddUpdateMeetingVM meetingToUpdate);
        List<MeetingVM> MultipleCandidateInvites(string emailid);
        Task<AddUpdateMeetingVM> GetMeeting(int id);
        Task<List<Meeting>> GetAllOpenInterviews(string emailID);
        List<Meeting> GetAllQCInterviews(string emailID);
        List<MeetingVM> GetAllClientInterviews(string emailID);

        Task<string> AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate);
        Task<bool> UnassignYourself(int meetingid, string email, bool isPenalty);
        Task<List<Meeting>> GetAllAssignedInterviews(string emailID);
        Task<bool> UpdateComments(InterviewComments comments, string performedBy = null);
        Task<InterviewComments> GetInterviewComments(int interviewId);
        Task<InterviewReport> GetInteviewReport(int interviewId, int irfId);
        Task<bool> AddInterviewReport(InterviewReport reportModel, string emailId);
        //IEnumerable<SkillComment> GetTechSkillComments(int IRFid);
        Task<IEnumerable<SkillComment>> GetTechSkillComments(int irfId, int userId = 0);
        SelectList GetIRFList();
        IEnumerable<TransClientUser> GetClientsByRoles(string IRF);
        Task<List<MeetingVM>> GetClientMeetingList(string emailId);
        bool PostApproval(string imid, string value, string comment);
        bool PostSelectionStatus(string imid, string value, string comment);
        bool SaveClientFeedback(int imid, int isApproved, string approvalComment, int isSelected, string selectionComment, int joiningStatus, string joiningComment, int offerStatus, string offerComment, bool movetoCP, string clientEmailId);
        Task<bool> SaveQCFeedback(string imid, string qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled, string oldQcStatus, string performedBy = null);
        Task<List<Meeting>> GetCandidateInterviews(string emailID, string status);
        Task<bool> CancelInterview(int meetingid, string interviewDate, string candidateEmail);
        List<string> GetClientHRContactList(string emailId, List<MeetingVM> meetingList);
        List<string> GetEndClientList(string emailId, List<MeetingVM> meetingList);
        List<string> GetIRFList(string emailId, List<MeetingVM> meetingList);
        Task<VerifyCandidateAudio> GetCandidateAudios(int candidateId, int irfId);
        string VerifyCandidateVoice(string userId, string fileUrl, string phrase, int meetingId, string type);

        Task<MeetingVM> GetMeetingbyid(int id, string Email);
        Task<List<MeetingVM>> GetRiseTubeMeetings(int clientId, string profile, string Emailid, bool IsClientCurated = false);
        Task<MeetingVM> VedioLike(string value, string vedioid, string email);
        Task AddTotalViewCount(int id, string emailaddress, string flag);
        //Task<string> VedioLikeAsync(string value, string vedioid, string email);
        Task<string> VedioShare(string value, string vedioid);
        Task<string> VedioShared(string value, string vedioid, string Userid);
        Task<string> VedioComment(string value, string vedioid, string email);
        Task<List<VedioComments>> GetvedioComments(int id);
        Task<EmailDetails> GetvedioShortList(string vedioid, string email);
        Task<string> CandidateSuitable(int id, string email, string Comment);
        Task<VedioComments> Intrestreaction(string vedioid, string Status);
        string ShortListreaction(string vedioid, string Status);
        Task<List<Meeting>> ShortListedClient(string emailid, string IRFID);
        Task<List<Meeting>> InterestListClient(string emailid, string IRFID);
        string Fetchusername(string emailid);
        Task<IEnumerable<VideoCountdetail>> GetTotalViewCount(int id);

        List<MeetingVM> IRFMultipleCandidateInvites(string IRFId);
        Task<bool> DeleteInterview(int imid);
        bool SaveFeedbackAudio(int interviewId, string irfid, string feedbackAudioLink, int candidateType = 0, bool isClientFeedback = false, string currentUser = null);
        Task<string> Broadcast(BroadcastModel model);
        IEnumerable<Meeting> GetHROpenInterviews(string emailId);
        string HRAssignYourself(int meetingid, string hrMailId);
        IEnumerable<Meeting> GetHRAssignedInterviews(string emailID);
        bool HRUnassignYourself(int meetingid);
        Task<bool> SaveHRComment(int id, string comment, string imageFileURL, string performedBy, string candidateNameEmail);
        Task<List<MeetingVM>> GetCPMeetings(int clientId, string profile, string Emailid);
        IEnumerable<int> GetCuratedGroupIRFs(int curatedGroupId);
        Task<string> AddSharedVideoDetails(InterviewReport reportModel);
        Task<int> SharedGetInterviewId(int videoid);
        Task<InterviewReport> GetSharedInteviewReport(int interviewId, int irfId, int id);
        IEnumerable<VideoDetails> GetVideoSumaryDetails(int Interviewid);
        Task AddTimeSpend(int id, string emailaddress, string flag, TimeSpan TimeSpend, string CurrentTime);
        IEnumerable<VideoDetails> GetallVideoSumaryDetails();
        string AddVedioShare(string email, string vedioid, string Emailids);
        Task<int> CheckVedioShare(string vedioid, string Emailid);
        string AddVideoIntrestDetails(string videoid, string UrlType, string Emailid);
        IEnumerable<VideoDetails> GetVideoIntrestDetails();
        IEnumerable<string> FetchShareEmails(int Interviewid);
        IEnumerable<VideoDetails> GetVideoCandidateSumaryDetails(int Interviewid, string emailid);
        string UpdateCandidateStatus(int videoid, string Status);
        Task<SentimentPercentage> GetSentimentAnalysis(int imid);
        Task<List<MeetingVM>> GetSharedRiseTubeMeetings();
        Task<List<QuestionDetails>> FetchQuestionDetails(int interviewid);
        string UpdateQuestionDetails(List<QuestionDetails> _question);


        Task<string> BroadcastIRFmembers(BroadcastModel model);

        IEnumerable<MeetingVM> GetClientJdIRFs(int Clientjdid, string emailId);
        Task<List<MeetingVM>> GetRiseTubeMeetingsByScore(int clientId, string profile, string Emailid, bool IsClientCurated = false);

        Task<List<MeetingVM>> GetRiseTubeMeetingsByIRFId(int clientId, string Emailid, string irfId, DateTime startDate, DateTime endDate, string profileName = null);
        //Task<List<MeetingVM>> GetRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern, string profileName = null);
        Task<List<MeetingVM>> GetRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern, DateTime startDate, DateTime endDate, string profileName = null);
        Task<List<MeetingVM>> GetRiseTubeMeetingsForCuratedUserByIRF(string Emailid, string actionStatus, int irfId);
        SurveyCandidateInterviewer FetchCanddiateInterviewer(int interviewid);
        string CandidateSuitableJd(int id, string email, string Comment, int Clientjdid);

        Task<List<MeetingVM>> GetRiseTubeMeetingsPaged(string profile, string Emailid, int Page, string searchValue = null, int clientId = 0, bool IsClientCurated = false);
        Task<List<MeetingVM>> GetSharedRiseTubeMeetingsPaged(int page, string searchValue = null, string profileName = null);
        Task<int> GetSharedIndexMeetingsCount(string searchValue = null, string profileName = null);
        Task<List<SurveyDetails>> FetchSurveyInterviewer(List<SurveyDetails> _lst);
        Task<List<MeetingVM>> GetSharedRiseTubeMeetingsForClientPagedFiltered(int page, string primarySkills, string heuristicLevels, string searchValue = null);
        Task<int> GetSharedIndexMeetingsCountforClientFiltered(string primarySkills, string heuristicLevels, string searchValue = null);
        Task<int> GetRisetubeIndexMeetingsCount(string profile, string Emailid, string searchValue = null, int clientId = 0, bool IsClientCurated = false);
        Task<List<MeetingVM>> GetRiseTubeMeetingsPagedFiltered(string profile, string Emailid, int Page, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0);
        Task<int> GetRisetubeIndexMeetingsFilteredCount(string profile, string Emailid, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0);
        Task<List<RBInterviewDetails>> RBDetails(string EmailId);
        Task<string> RBUpdateStatusDetails(string EmailId);
        Task<List<RBInterviewDetails>> FetchFNDetails(string EmailId);
        Task<List<RBInterviewDetails>> RBInterviewerDetails(int interviewerid, int flag);
        Task<string> RBAddPoints(int interviewerid, int Points, string Comment, DateTime PointDate);
        Task<string> RBConfirmPayment(int interviewerid, int index, DateTime st, DateTime se, int paymenthistory);
        Task<List<string>> FetchInterviewerList(string searchterm);
        Task<string> InsertAdvancePayment(string Interviewer, string Comment, int Amount, int PaymentHistoryId);
        Task<List<RBInterviewDetails>> FNGenerateInvoiceDetails(DateTime st, DateTime se);
        Task<string> LockorUnlockVideo(string imid, int islock, string emailid);
        Task<List<string>> FetchClientUsers(string ClientName);
        Task<string> SetCandidateAvailability(string imid, int isavailable);
        Task<string> MoveToCP(string imid, int movetocp);
        Task<List<MeetingVM>> GetAllSelectedCandidateInterviewList(DateTime startDate, DateTime endDate, string email);
        Task<List<RBInterviewDetails>> FetchAllFNDetail();
        Task<List<RBInterviewDetails>> FNAllGeneratedInvoiceDetails(DateTime st, DateTime se);
        Task<List<int>> FetchAllInterviewidfromIrf(string IRFid);
        IEnumerable<int> FetchAllInterviewidByClient(int Clientid, string emailid);
        IEnumerable<SurveyDetails> FetchSurveyClientName(List<SurveyDetails> _lst);
        IEnumerable<SurveyDetails> FetchSurveyCandidateEmails(List<SurveyDetails> _lst);

        Task<string> InsertPaymentHistory(string Interviewer, int Amount, int TotalAmount, string actionvalue, string Comment, string transferId = null, string cfTransferId = null, string payoutStatus = null);
        Task<List<RBInterviewDetails>> FetchFNPaymentHistory();

        /// <summary>
        /// Re-initiates a Cashfree payout for a PaymentHistory row whose <c>PayoutStatus</c>
        /// is <c>TRANSFER_FAILED</c>. Inserts a new PaymentHistory row for the retry attempt
        /// and returns (success, message).
        /// </summary>
        Task<Tuple<bool, string>> RetryPaymentViaCashfree(int paymentHistoryId);
        Task<IEnumerable<MeetingVM>> GetClientSelectedMeetings(string emailId, bool isFilter = false, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Meeting>> FetchOpenMeetingById(string id);
        string ReassignMeeting(string id, string meetingid, string Emailid);
        string ReassignMeetingByTeam(string id, string meetingid, string Emailid);
        Task<InterviewerDetails> FetchInterviewerCodedName(string emailID);
        Task<SelectList> GetIRFInterviewingMembersEmails(string irfId);
        Task<SelectList> GetIRFQualityControllersEmails(string irfId);
        Task<List<ChannelMembersForInterviewData>> GetIRFMembersEmailsForInterviewDetails(string irfId);
        Task<InterviewerRating> FetchRatingDetails(int interviewId, string emailid);
        Task<string> SaveRatingDetails(InterviewerRating model);
        Task<List<MeetingVM>> GetClientNotScheduledMeetingList(int Clientid, string Emailid);
        Task<List<InvoiceDetailsVM>> FetchFNInvoiceDetails(string Startdate, string Enddate);
        Task<InvoiceDetailsVM> GetInvoiceDetailsById(int id);
        Task<string> SaveInvoiceDetails(InvoiceDetailsVM model);
        Task<List<InvoiceListVM>> GetInvoiceList(int month, int year);
        Task<ClientInvoiceFormDataVM> GetClientInvoiceFormData(int clientId, int month, int year, int billingOwnerId = 0);
        Task<List<BillingOwnerOption>> GetClientBillingOwners(int clientId);
        Task<DuplicateInvoiceCheckVM> CheckDuplicateInvoice(int clientId, int billingOwnerId, int month, int year);
        Task<InvoiceDetailVM> GetGeneratedInvoiceById(int invoiceId);
        Task<List<InvoiceListVM>> GetUnpaidInvoiceList();
        Task<List<InvoiceListVM>> GetPaidInvoiceList(int fromYear = 0, int fromMonth = 0, int toYear = 0, int toMonth = 0);
        Task<bool> SaveInvoiceExpectedDate(int invoiceId, DateTime? expectedDate);
        Task<bool> UpdateInvoiceDetails(int invoiceId, string gstNumber, string creditPeriod, string irn, string eInvoiceFileLink, int status,
            decimal subtotal, decimal gstAmount, decimal invoiceValue, decimal tdsAmount, decimal netReceivable,
            List<InvoiceLineItemVM> lineItems, string latestRemark = null, string utrTrxn = null, decimal? cashCreditAmount = null, int taxScenario = 0);
        Task<string> GetNextInvoiceNumber(int month, int year);
        Task<int> SaveGeneratedInvoice(GenerateInvoiceVM model);
        List<MeetingVM> GetClientNotScheduledMeetingListByIRFId(string IRFId, string emailId = null);
        Task<List<MeetingVM>> GetClientReScheduledMeetingList(int Clientid, string Emailid, DateTime startdate, DateTime enddate);
        List<CallCatalogueVM> GetCallCatalogueList(string profile, string Emailid, InterviewFilter filter = null);
        InterviewFilter PopulateCallCatalogueFilterFacets(List<CallCatalogueVM> meetings, InterviewFilter filter);
        List<string> FetchClientListByIRFID(int IRFID);
        IEnumerable<int> FetchAllInterviewidBySeniorHR(string emailid);
        Task<List<RBInterviewDetails>> GetPaymentHistoryDetailsView(int interviewerid);
        Task<List<RBInterviewDetails>> FNFetchToalAmountPaid(DateTime st, DateTime se);
        Task<List<RBInterviewDetails>> FNFetchToalAmountStatus(DateTime st, DateTime se);
        Task<List<RBInterviewDetails>> FNFetchToalQCAmountStatus(DateTime st, DateTime se);
        Task<List<SchedulingLinkVM>> FetchSchedulingLinks(string emailid, string type);
        Task<bool> PublicCancelInterview(int meetingid, string loggedInUser = null);
        Task<int> GetUniqueInterviewerByEmailId(string emailId, string id);
        Task<List<UnassignedInterviewVM>> FetchAllUnassignedInterviewes();
        bool UpdateCancelInterviewDetails(int meetingid);
        Task<List<UnassignedInterviewVM>> FetchCandidateMeetingsForReminder();
        Task<string> UpdateCandidateMeetingsStatus(int meetingid, int status, string uniqueid, int Irfid, int candidateId, bool response, string cancellationreason);
        Task<List<CustomerDetailsVM>> FetchCustomerDetails();
        Task<List<RBInterviewerPayoutDetails>> FNAllGeneratedPayoutDetails(DateTime st, DateTime se);
        Task<List<RBInterviewerPayoutDetails>> FetchAllPayoutDetails();
        Task<bool> UpdateCheckIntervieweravailbilityStatus(int status, string uniqueid, int Irfid);

        Task<Tuple<bool, string>> UpdateInterviewerAvailbilityStatusForIRF(int status, string uniqueid, int Irfid, string rsc = null);

        Task<bool> UpdateInterviewerAvailbilityStatusForAssessmentGroup(bool status, string uniqueid, int agId, string rsc = null);
        string SetInterviewerAvailability(int transid, int isavailable);
        Task<List<InterviewerRating>> FetchFeedbackDetails();
        Task<List<InterviewerRating>> FetchFeedbackDetailsByIRFId(string IRFId);
        bool ChangeInterviewerNotificationStatus(string emailID, int status);
        Task<bool> SendsMailsAfterCancellingInterview(int meetingId, int irfId, string candidateEmail);
        Task<List<MeetingVM>> GetCompletedInterviewList(DateTime startDate, DateTime endDate);
        Task<QCFormVM> FetchQCFormDetailsById(int interviewid);
        Task<bool> AddorUpdateQCFormReport(QCFormVM model);
        List<CallCatalogueVM> GetInterviewerCallCatalogueList(InterviewFilter filter = null);
        Task<bool> ApplyPenaltyQCForm(int interviewid, string interviewer, int redflags, int VideoReward);
        Task<string> GetInterviewerEmailByInterviewId(int interviewId);
        Task<bool> CanApplyQcPenaltyToInterviewPointsAsync(int interviewId, string interviewerEmail);
        Task<string> GetClientHRContactByInterviewid(int interviewid);
        Task<List<QCFormVM>> FetchAllQCFormDetails(DateTime startDate, DateTime endDate);
        Task<List<MeetingVM>> QCReportDetailsByInterviewer(string emailId);
        Task<bool> UpdateQCReportRedFlagsResolved(int interviewid);
        Task<List<MeetingVM>> GetReslovedRedFlagsInterviewList();
        Task<List<FnRevenueVM>> FetchRevenueDetails(DateTime stdate, DateTime endate, int fromYear, int fromMonth, int toYear, int toMonth);
        Task<List<FnRevenueVM>> FetchRevenueDetailsByClient(int clientid, DateTime stdate, DateTime endate, int billingType, string year, int month);
        Task<List<FnRevenueVM>> FetchAllRevenueDetails(DateTime stdate, DateTime endate);
        Task<MeetingVM> GetInterviewMeetingById(int id);
        Task<bool> SaveVideoDetailsById(int interviewid, string VideoURL, int videoduration, string CandidateEmail, string emailId);
        Task<FnRevenueVM> FetchRevenueStatusDetails(int clientid, string year, int month, int billingType, string Detailid);
        Task<bool> SaveRevenueStatusDetails(int clientid, string year, int month, int billingType, string Detailid, int currentStatus, int invoiceamount, int poPending, string Emailid, int invoicerealization, int invoicesubmit);
        Task<List<string>> FetchCandidateZoomMeetingsIdForCancel(string MeetingPlatForm);
        Task<RedoInterviewVM> GetInterviewDetailsById(int id);
        Task<bool> SaveRedoInterviewDetails(RedoInterviewVM model);
        Task<RedoInterviewVM> GetRedoInterviewDetailsById(int id);
        Task<bool> UpdateRejectRedoInterviewDetails(RedoInterviewVM model);
        Task<bool> ResetInterviewPayment(int id);
        Task<bool> CheckInterviewCancellable(int meetingid);
        Task<string> GetMeetingPlatformbyIRFId(string IRFId);
        Task<List<MeetingVM>> GetRedoInterviewList();
        bool CheckInterviewClerityTime(int meetingid);
        Task<string> StoreClientBroadcastMessage(BroadcastModel model);
        Task<IEnumerable<CallRecordingDataVM>> GetCallRecordingData(string profile, string Emailid);
        Task<bool> CheckReportClientShareDetails(string Emailid, string uniqueid, int Interviewid);
        Task<string> CheckReportClientUrlSave(string Emailid, string uniqueid, int Interviewid);
        Task<bool> SaveInterviewsReview(int rating, string comments, string Emailid);
        Task<IEnumerable<ClientReviewVM>> FetchInterviewsReview();
        Task<bool> BacklinkFailInterviewStatusUpdate(int id);
        Task<IEnumerable<MeetingVM>> GetProbableNoShowCandidates(string Emailid);
        Task<bool> SaveSauveyIRFReview(int rating, string comments, string Emailid, string IRFId);
        Task<bool> SaveCapturedJoinMeetingTime(int interviewid, string emailid);
        Task<List<Meeting>> GetInterviewerAssignedInterviews(string emailID, string meetingLineUpType);
        Task<Meeting> GetMeetingDetails(int interviewId);
        Task<bool> SaveSurchargeInterviewDetailsById(int interviewid, string surchargecomment, int surchargeamt, string emailId, string CandidateEmail, int IRFId);
        Task<List<MeetingVM>> FetchCompletedInterviewsByClientId(int clientid, DateTime stdate, DateTime endate, string year, int month);
        Task<string> GetTranscriptUrlByMeeting(int meetingId);
        Task<bool> AddBlackListCompanyService(BlackListCompanyVM model);
        Task<PaymentSLAVM> FetchPaymentSlaDetailsById(int interviewid, string type);
        Task<IEnumerable<PaymentSLAVM>> FetchPaymentSlaHistoryDetailsById(int interviewid);
        Task<bool> AddorUpdatePaymentSLADetails(PaymentSLAVM model);
        Task<IEnumerable<PaymentSLAVM>> FetchPaymentSlaDetails(string actionstatus);
        Task<bool> PaymentSlaNotApproved(int interviewid, string emailid);
        Task<bool> AgreeToPaymentSLAConditions(string emailid);
        Task<bool> AddTransInterviewersReliabilityScoresService(TransInterviewersReliabilityScoresVm model);
        Task<bool> AddTransReliabilityScoresSelectAndRejectService(int InterviewMeetingId);
        Task<Meeting> GetAudioCommentDetailsService(int interviewId);
        Task<bool> RemoveOrShortlistInterviewer(int interviewid, string action = null);
        Task<bool> RemoveOrShortlistInterviewerFromIRF(int irfid, string interviewer, string action = null);
        Task<bool> UpdateInterviewReport(int interviewid, string comment, string emailid);
        Task<bool> SaveProctoringReport(ProctoringReport model, string emailId);
        Task<ProctoringReport> GetProctoringReport(int interviewid);
        Task<IEnumerable<MeetingVM>> GetUnassignedOpenInterviews(int irfId);
        Task<int> GetUniqueUserByEmailId(string emailId, string id);
        Task<bool> IsInterviewerForInterview(string emailId, int interviewId);
        Task<bool> ConfirmInterviewavailability(string emailId, int id, string type);
        Task<bool> UpdateInterviewerAvailbilityStatusCampaignLevel(bool status, string uniqueid, int campaignId, string rsc = null);
        Task<bool> MoveInterviewForTechQC(string Emailid, int interviewid, bool activestatus);
        Task<List<MeetingVM>> GetQCOpenInterviews(string emailId);
        Task<bool> AssignYourSelfForMyQCInterviws(string emailId, int id);
        Task<List<MeetingVM>> GetMyQCInterviews(string emailId);
        Task<List<DMInstructionVM>> GetMyDMInstruction(string emailId);
        Task<bool> SaveOpenAIReportQC(int interviewId, string openAIQcResponseJson);
        Task<bool> SaveOpenAIReportQCV2(int interviewId, AIReportQCAnalysisSchema model);
        Task<AIReportQCAnalysisSchema> GetOpenAIReportQC(int interviewId);
        Task<int> checkInterviewEditable(int meetingid);
        Task<bool> DeleteQuestionsDiscussedFromReport(int interviewId);
        Task<string> GetInterviewCancellationSource(int interviewid);
        Task<bool> SaveAITranscriptAnalysis(int interviewId, TranscriptAnalysisSchema model);
        Task<bool> SaveQcInterviewAIevaluation(int interviewId, TranscriptAnalysisSchema model, string jsonResponse = null, bool transcritAi = true, string reportAIJsonResponse = null);
        Task<IEnumerable<QcInterviewAIMergePendingRow>> GetPendingQcInterviewAIMerge();
        Task<QcInterviewAIMergePendingRow> GetPendingQcInterviewAIMergeById(int interviewid);
        Task<bool> SaveQcInterviewAIMergedDetails(int qcEvaluationId, string mergedJson, int? totalPenalty = null, string interviewerSuspicious = null, bool? redoInterview = null);
        Task<bool> SaveAiContradictionDetails(int qcEvaluationId, string contradictionJson = null);
        Task<QcInterviewAIMergePendingRow> CheckForAiContradiction(int qcEvaluationId);
        Task<bool> UpdateTranscriptSummary(int interviewId, string transcriptSummary = null);
        Task<QcInterviewAIevaluation> GetQcInterviewAIevaluationByInterviewId(int interviewId);
        Task<string> CandidateMeetingsStatus(int meetingid, int status, string uniqueid, int Irfid, int candidateId, string cancellationreason);
        Task<UnassignedInterviewVM> GetCandidateInterviewDetails(string uniqueId, int irfId, int interviewId, int candidateId);
        Task<UnassignedInterviewVM> GetInterviewerInterviewDetails(int interviewId);
        Task<bool> SaveAIInterviewProctorEvaluation(InterviewReport model);
        Task<IEnumerable<ZohoParticipantList>> GetZohoLiveMeetingInfomation();
        Task<string> GetQuestionSkillById(string questionText, int irfId, string emailId);
        Task<IEnumerable<MeetingVM>> FetchMoreDetailsInterview(int interviewId);
        Task<IEnumerable<PaymentSLAVM>> GetAutoApprovalPaymentSLAPendingList();

        /// <summary>
        /// Validates bank details, initiates a Cashfree bank payout, and marks interview points as paid.
        /// </summary>
        Task<Tuple<bool, string>> ConfirmPaymentViaCashfree(int id, int index, string email, string amount, DateTime st, DateTime se);

        /// <summary>
        /// Fetches the current transfer status from Cashfree for the given transferId.
        /// </summary>
        Task<CashfreePayoutResult> GetCashfreePayoutStatus(string transferId);

        CapCheckResult CheckInterviewerCapForAssignmentByMeeting(int meetingId, string interviewerEmail);

        /// <summary>The Interview Alignment briefing, shaped for display. Never returns null.</summary>
        Task<InterviewAlignmentVM> GetInterviewAlignmentDetails(int interviewId, string interviewerEmail);

        /// <summary>Records the one-time trial onboarding acknowledgement.</summary>
        Task<bool> SaveTrialOnboardingAcknowledgement(string interviewerEmail, string policyVersion);

        /// <summary>Creates one code session with a cryptographically random token.</summary>
        Task<CodeSessionVM> CreateCodeSession(CodeSessionVM session, string interviewerEmail);

        /// <summary>Resolves a candidate link. Never returns null.</summary>
        Task<CodeSessionCandidateVM> GetCodeSessionByToken(string sessionToken);

        /// <summary>Debounced autosave. Leaves any stored run output untouched.</summary>
        Task<bool> SaveCodeSessionProgress(string sessionToken, string code, string language, int pasteEvents);

        /// <summary>Runs the code and stores the result against the session.</summary>
        Task<CodeExecutionResult> RunCodeSession(string sessionToken, string code, string language, int pasteEvents);

        /// <summary>Re-runs once and submits, so stored output always matches submitted code.</summary>
        Task<bool> SubmitCodeSession(string sessionToken, string code, string language, int pasteEvents);

        /// <summary>Every session for an interview - the interviewer's view.</summary>
        Task<List<CodeSessionVM>> GetCodeSessionsForInterview(int interviewId);

        /// <summary>Submitted attempts only, ordered for the client report.</summary>
        Task<List<CodeSessionVM>> GetSubmittedCodeSessionsForReport(int interviewId);
    }
}
