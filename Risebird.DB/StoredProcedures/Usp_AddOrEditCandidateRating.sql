  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_AddOrEditCandidateRating'))
BEGIN
    DROP PROCEDURE Usp_AddOrEditCandidateRating
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure Usp_AddOrEditCandidateRating
@EmailAddress varchar(200),
@interviewid int,
@Question1 int,
@Question2 int,
@Question3 int,
@Question4 varchar(1000),
@Question5 varchar(1000),
@Question6 int,
@Type varchar(20),
@Comment varchar(1000),
@Name varchar(100),
@Phone varchar(20)
AS
BEGIN
Declare @RatingCount int
Declare @Interviewerid int
Select @Interviewerid=ul.id from interviewmeetings im join userlist ul on im.interviewer=ul.emailid where im.id=@interviewid

Select @RatingCount=Count(id) From CandidateRatings where interviewid=@interviewid
	if(@RatingCount>0)
	begin
	Update CandidateRatings set Question1=@Question1,Question2=@Question2,Question3=@Question3,Question4=@Question4,
	Question5=@Question5,Question6=@Question6,Comment=@Comment,Phone=@Phone,Name=@Name
	where interviewid=@interviewid
	end
	else
	begin
	Insert into CandidateRatings(interviewid,Question1,Question2,Question3,Question4,Question5,Question6,Interviewerid,CreatedOn,Type,Comment,Name,Phone) 
	values(@interviewid,@Question1,@Question2,@Question3,@Question4,@Question5,@Question6,@Interviewerid,Getdate(),@Type,@Comment,@Name,@Phone)
	end
Select CandidateEmail,Interviewer,ul.id,ir.JDTiltle as PrimarySkill,(ulc.FirstName) as CandidateName,
Case When @RatingCount>0 then 1 else 0 end as IsUpdated 
from interviewmeetings im 
join IRF ir on ir.IRFId=im.IRFId 
left join userlist ul on im.interviewer=ul.emailid 
left join Userlist ulc on ulc.Emailid=im.CandidateEmail 
where im.id=@interviewid
END
GO