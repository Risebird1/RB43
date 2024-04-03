
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMyDMInstruction'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMyDMInstruction
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetMyDMInstruction
@emailID varchar(150)
As
BEGIN
Select IRF.id,IRF.IRFId,IRF.JDTiltle,IRF.JDLink,IRF.StartDate,uli.EmailId as ChannelOwner from IRF join SkillChannelSMEs smm on smm.IRFId=IRF.id 
join InterviewerProfile ip on ip.id=smm.InterviewerProfileId
join UserList ul on ul.id=ip.UserId
left join UserList uli on IRF.RisebirdHr=uli.id
where ul.EmailId=@emailID and ISNULL(DMApproved,0)=0
order by IRF.StartDate desc
END
GO