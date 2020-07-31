/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerCount]    Script Date: 27-07-2020 7.37.52 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerCount
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetInterviewerCount]  
AS  
Begin 

select count(id) as InterviewerCount, (select count(id) from InterviewerProfile where IsApproved=1) as ApprovedCount,  
(select count(id) from InterviewerProfile where IsApproved=0) as UnapprovedCount
from InterviewerProfile

End

GO


