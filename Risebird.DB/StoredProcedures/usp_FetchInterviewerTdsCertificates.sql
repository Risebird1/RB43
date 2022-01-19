IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchInterviewerTdsCertificates'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewerTdsCertificates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchInterviewerTdsCertificates  
@Emailid varchar(150)  
As  
BEGIN  
Select ic.id,ic.interviewerid as InterviewerId,ic.year,ic.month,ic.comment,ic.certificatelink ,ul.EmailId as emailid From InterviewerCertificates ic join Userlist ul on ul.id=ic.interviewerid where ul.EmailId=@Emailid  
END
GO