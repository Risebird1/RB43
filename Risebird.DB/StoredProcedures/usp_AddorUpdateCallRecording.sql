IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddorUpdateCallRecording'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddorUpdateCallRecording]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_AddorUpdateCallRecording
@interviewid int,
@IRFId varchar(50),
@RecordingUrl varchar(1000),
@Sid varchar(50),
@CandidateEmail varchar(150),
@WhoCalled varchar(150)
AS
BEGIN

declare  @numericIRFId varchar(50)

if(@IRFId is not null)
Begin
if(ISNUMERIC(@IRFId) = 0)
Begin
set @numericIRFId = (select id from IRF where IRFId = @IRFId)
End
Else
Begin
set @numericIRFId = @IRFId
End
End

if not Exists (Select 1 from CallRecordingDetails where Sid=@Sid)
BEGIN
Insert into CallRecordingDetails(Interviewid,IRFId,RecordingUrl,Sid,CandidateEmail,WhoCalled,CalledOn)   
values(@Interviewid,@numericIRFId,@RecordingUrl,@Sid,@CandidateEmail,@WhoCalled,dateadd(minute,330,GetDate()))  
END
Else
Begin
Update CallRecordingDetails set RecordingUrl=@RecordingUrl where Sid=@Sid
End

END