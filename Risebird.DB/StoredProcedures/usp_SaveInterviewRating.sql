IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewRating'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveInterviewRating]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create Procedure usp_SaveInterviewRating
@Emailid varchar(150),
@Interviewid int,
@StarRating int,
@Comment varchar(400)
AS
BEGIN
Declare @Userid int
Select @Userid=id from Userlist where Emailid=@Emailid
if EXISTS (Select 1 from InterviewRatings where Userid=@Userid and Interviewid=@Interviewid)
Begin
Update InterviewRatings set StarRating=@StarRating,Comment=@Comment,Addedon=GetDate() where Userid=@Userid and Interviewid=@Interviewid
END
Else 
Begin
Insert into InterviewRatings (Interviewid,Userid,Comment,Addedon,StarRating) 
values(@Interviewid,@Userid,@Comment,GetDate(),@StarRating)
END
END

GO