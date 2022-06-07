IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddorUpdateQCFormReport'))
BEGIN
    DROP PROCEDURE dbo.usp_AddorUpdateQCFormReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_AddorUpdateQCFormReport
@Interviewid int,
@Emailid varchar(200),
@IsCodingPresent bit,
@IsVideoClearQuality bit,
@IsInterviewerCommunicationClear bit,
@IsSufficientVideoDuration bit,
@IsInterviewStructured bit,
@IsScoringrubriccorrect bit,
@IsQuestionswithtime bit,
@IsCommentfilled bit,
@IsMeetingStatusfilled bit,
@IsRelevancequestions bit,
@IsDelayresuminginterview bit,
@IsBackgroundnoise bit,
@IsPoorinterviewerinfrastructure bit,
@IsPoorhandlingcandidatequery bit,
@IsPersonalquestions bit,
@IsAugmenting bit,
@IsBiasedView bit,
@IsLettingcandidatespeak bit,
@IsSuspiciousappfound bit,
@IsLipsyncissue bit
AS
BEGIN
Declare @Userid int 
Select @Userid=id From UserList where Emailid=@Emailid
IF EXISTS(Select 1 from Trans_QCFormReport where Interviewid=@Interviewid)
begin
Update Trans_QCFormReport set IsCodingPresent=@IsCodingPresent,IsVideoClearQuality=@IsVideoClearQuality,IsInterviewerCommunicationClear=@IsInterviewerCommunicationClear,
IsSufficientVideoDuration=@IsSufficientVideoDuration,IsInterviewStructured=@IsInterviewStructured,IsScoringrubriccorrect=@IsScoringrubriccorrect,
IsQuestionswithtime=@IsQuestionswithtime,IsCommentfilled=@IsCommentfilled,IsMeetingStatusfilled=@IsMeetingStatusfilled,
IsRelevancequestions=@IsRelevancequestions,IsDelayresuminginterview=@IsDelayresuminginterview,IsBackgroundnoise=@IsBackgroundnoise,
IsPoorinterviewerinfrastructure=@IsPoorinterviewerinfrastructure,IsPoorhandlingcandidatequery=@IsPoorhandlingcandidatequery,
IsPersonalquestions=@IsPersonalquestions,IsAugmenting=@IsAugmenting,IsBiasedView=@IsBiasedView,IsLettingcandidatespeak=@IsLettingcandidatespeak,
IsSuspiciousappfound=@IsSuspiciousappfound,IsLipsyncissue=@IsLipsyncissue Where Interviewid=@Interviewid
end
Else
begin
Insert into Trans_QCFormReport(Interviewid,Userid,IsCodingPresent,IsVideoClearQuality,IsInterviewerCommunicationClear,IsSufficientVideoDuration,
IsInterviewStructured,IsScoringrubriccorrect,IsQuestionswithtime,IsCommentfilled,IsMeetingStatusfilled,IsRelevancequestions,IsDelayresuminginterview,
IsBackgroundnoise,IsPoorinterviewerinfrastructure,IsPoorhandlingcandidatequery,IsPersonalquestions,IsAugmenting,IsBiasedView,IsLettingcandidatespeak,
IsSuspiciousappfound,IsLipsyncissue,Addedon) values(@Interviewid,@Userid,@IsCodingPresent,@IsVideoClearQuality,@IsInterviewerCommunicationClear,@IsSufficientVideoDuration,
@IsInterviewStructured,@IsScoringrubriccorrect,@IsQuestionswithtime,@IsCommentfilled,@IsMeetingStatusfilled,@IsRelevancequestions,@IsDelayresuminginterview,
@IsBackgroundnoise,@IsPoorinterviewerinfrastructure,@IsPoorhandlingcandidatequery,@IsPersonalquestions,@IsAugmenting,@IsBiasedView,@IsLettingcandidatespeak,
@IsSuspiciousappfound,@IsLipsyncissue,GetDate())
end
END
GO