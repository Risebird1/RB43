IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CarryForwardInterviewerPayments'))
BEGIN
    DROP PROCEDURE dbo.usp_CarryForwardInterviewerPayments
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_CarryForwardInterviewerPayments
@Userid int,
@st datetime,
@se datetime,
@NextDate datetime
AS
BEGIN
Update ip Set ip.CarryForwardDate=@NextDate
from InterviewPoints ip left join InterviewMeetings im on im.id=ip.Interviewid
where ip.userid=@Userid and (ip.Status='Submitted' or ip.Status='Pending') and ip.interviewid!=0
and (isNull(ip.CarryForwardDate,isNull(im.interviewdate,ip.Date))>=@st and isNull(ip.CarryForwardDate,isNull(im.interviewdate,ip.Date))<=@se)

Update ip Set ip.CarryForwardDate=@NextDate
from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id=ip.Interviewerid
where ip.userid=@Userid and (ip.Status='Submitted' or ip.Status='Pending') and ip.interviewerid!=0
and (isNull(ip.CarryForwardDate,isNull(im.interviewdate,ip.Date))>=@st and isNull(ip.CarryForwardDate,isNull(im.interviewdate,ip.Date))<=@se)

Update ip Set ip.CarryForwardDate=@NextDate
from InterviewPoints ip
where ip.userid=@Userid and (ip.Status='Submitted' or ip.Status='Pending') and ip.interviewerid=0 and ip.interviewid=0
and (isNull(ip.CarryForwardDate,ip.Date)>=@st and isNull(ip.CarryForwardDate,ip.Date)<=@se)
END
GO
