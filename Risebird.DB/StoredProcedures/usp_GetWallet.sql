IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetWallet'))
BEGIN
    DROP PROCEDURE dbo.usp_GetWallet
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetWallet
@BillingType int,          
@ClientId int = NULL,          
@BillingOwnerId int = NULL,    
@Action varchar(20) = NULL -- 'CREDIT', 'DEBIT', 'all'   
AS            
BEGIN            
Declare @ClientRoleId int  
Select @ClientRoleId=RoleId From ClientUser where UserId=@BillingOwnerId  
 SELECT     
  w.id,    
  w.BillingType,    
  w.ClientId,    
  w.BillingOwnerId,    
  w.CreatedBy,    
  w.Credit,    
  w.Debit,    
  w.CreatedOn,    
  w.Comment,    
  w.Balance,    
  w.InterviewId,    
  w.IRFId,    
  w.TransactionType,
  w.TransactionDate,
  w.TransactionId,
  w.InvoicePDF,
  c.Name as ClientName,    
  ul.EmailId as BillingOwnerEmail,    
  ul.FirstName + ' ' + ul.LastName as BillingOwnerName,    
  im.candidateEmail as CandidateEmail,    
  im.interviewDate as InterviewDate,    
  irf.IRFId as IRFName,    
  irf.JDTiltle as JobTitle,    
  irf.JDTitleTwo as JobTitleTwo,    
  CASE     
   WHEN ul_candidate.LastName IS NULL THEN ul_candidate.FirstName    
   ELSE RTRIM(LTRIM(ul_candidate.FirstName)) + ' ' + RTRIM(LTRIM(ul_candidate.LastName))    
  END as CandidateName,    
  ic.MeetingStatus as MeetingStatus    
 FROM Wallet w    
 LEFT JOIN Client c ON c.id = w.ClientId    
 LEFT JOIN UserList ul ON ul.id = w.BillingOwnerId    
 LEFT JOIN InterviewMeetings im ON im.id = w.InterviewId    
 LEFT JOIN IRF irf ON irf.id = w.IRFId    
 LEFT JOIN UserList ul_candidate ON ul_candidate.EmailId = im.candidateEmail    
 LEFT JOIN InterviewComments ic ON ic.InterviewId = w.InterviewId    
 WHERE w.BillingType = @BillingType    
  AND (@ClientId IS NULL OR w.ClientId = @ClientId)    
  AND (@BillingOwnerId IS NULL OR @ClientRoleId=9 OR w.BillingOwnerId = @BillingOwnerId)    
  AND (@Action = 'All'    
   OR (@Action = 'CREDIT' AND w.Credit > 0)    
   OR (@Action = 'DEBIT' AND w.Debit > 0))    
 ORDER BY w.CreatedOn DESC    
END     
GO

