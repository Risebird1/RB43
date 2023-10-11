IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddRedoInterviewDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddRedoInterviewDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_AddRedoInterviewDetails
@interviewid int,
@CandidateFace varchar(20),
@RequiredDuration int,
@ActualDuration int,
@Inappropriate varchar(20),
@HandsOfCoding varchar(20),
@Comments varchar(500),
@ModifiedBy varchar(200)
AS
BEGIN
IF EXISTS(select * from RedoInterviews where interviewid=@interviewid)
BEGIN
Update RedoInterviews set  interviewid=@interviewid,CandidateFace=@CandidateFace,Inappropriate=@Inappropriate,RequiredDuration=@RequiredDuration,
ActualDuration=@ActualDuration,HandsOfCoding=@HandsOfCoding,Comments=@Comments,RejectionComment=@RequiredDuration,ModifiedOn=GETDATE(),ModifiedBy=@ModifiedBy
where interviewid=@interviewid
END
ELSE
BEGIN
Insert into RedoInterviews(interviewid,CandidateFace,Inappropriate,RequiredDuration,ActualDuration,HandsOfCoding,Comments,
RejectionComment,Createdon,CreatedBy,ModifiedOn,ModifiedBy) values
(@interviewid,@CandidateFace,@Inappropriate,@RequiredDuration,@ActualDuration,@HandsOfCoding,@Comments,
null,Getdate(),@ModifiedBy,Getdate(),@ModifiedBy)
END 
END
Go