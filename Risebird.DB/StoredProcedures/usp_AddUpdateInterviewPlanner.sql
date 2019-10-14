IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateInterviewPlanner'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateInterviewPlanner
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddUpdateInterviewPlanner]

@IRFId int,
@InviteOwnedBy varchar(max),
@ConfirmationOwnedBy varchar(max),
@CompletionOwnedBy varchar(max),
@RisebirdSelectionOwnedBy varchar(max),
@ApprovalOwnedBy varchar(max),
@FinalSelectionOwnedBy varchar(max),
@OfferOwnedBy varchar(max),
@JoiningOwnedBy varchar(max),
@OverAllProcessingTime int,
@InviteProcessingTime int,
@ConfirmationProcessingTime int,
@CompletionProcessingTime int,
@RisebirdSelectionProcessingTime int,
@ApprovalProcessingTime int,
@FinalSelectionProcessingTime int,
@OfferProcessingTime int,
@JoiningProcessingTime int


AS

if exists(Select * from InterviewPlanner where IRFId = @IRFId)
begin 
update InterviewPlanner set 
[OverAllProcessingTime]=@OverAllProcessingTime,
[InviteProcessingTime]=@InviteProcessingTime,
[ConfirmationProcessingTime]=@ConfirmationProcessingTime,
[CompletionProcessingTime] = @CompletionProcessingTime,
[RisebirdSelectionProcessingTime]=@RisebirdSelectionProcessingTime,
[ApprovalProcessingTime]=@ApprovalProcessingTime,
[FinalSelectionProcessingTime] = @FinalSelectionProcessingTime,
[OfferProcessingTime] = @OfferProcessingTime,
[JoiningProcessingTime]=@JoiningProcessingTime,
[InviteOwnedBy] =@InviteOwnedBy,
[ConfirmationOwnedBy]=@ConfirmationOwnedBy,
[CompletionOwnedBy]=@CompletionOwnedBy,
[RisebirdSelectionOwnedBy]=@RisebirdSelectionOwnedBy,
[ApprovalOwnedBy]=@ApprovalOwnedBy,
[FinalSelectionOwnedBy]=@FinalSelectionOwnedBy,
[OfferOwnedBy]=@OfferOwnedBy,
[JoiningOwnedBy]=@JoiningOwnedBy

where IRFId=@IRFId

end
else
begin

INSERT INTO [dbo].[InterviewPlanner]
           ([IRFId]
           ,[OverAllProcessingTime]
           ,[InviteProcessingTime]
           ,[ConfirmationProcessingTime]
		   ,[CompletionProcessingTime]
           ,[RisebirdSelectionProcessingTime]
           ,[ApprovalProcessingTime]
           ,[FinalSelectionProcessingTime]           
           ,[OfferProcessingTime]
           ,[JoiningProcessingTime]
           ,[InviteOwnedBy]
           ,[ConfirmationOwnedBy]
           ,[CompletionOwnedBy]  
		   ,[RisebirdSelectionOwnedBy]
		   ,[ApprovalOwnedBy]
		   ,[FinalSelectionOwnedBy]
		   ,[OfferOwnedBy]
		   ,[JoiningOwnedBy]
		  
		   )
       VALUES
           (@IRFId,
@OverAllProcessingTime,
@InviteProcessingTime,
@ConfirmationProcessingTime,
@CompletionProcessingTime,
@RisebirdSelectionProcessingTime,
@ApprovalProcessingTime,
@FinalSelectionProcessingTime,
@OfferProcessingTime,
@JoiningProcessingTime,
@InviteOwnedBy,
@ConfirmationOwnedBy,
@CompletionOwnedBy,
@RisebirdSelectionOwnedBy,
@ApprovalOwnedBy,
@FinalSelectionOwnedBy,
@OfferOwnedBy,
@JoiningOwnedBy)


End

GO

