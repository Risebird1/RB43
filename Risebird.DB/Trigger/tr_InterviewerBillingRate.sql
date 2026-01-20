
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewerBillingRate')
BEGIN
    Drop trigger tr_InterviewerBillingRate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER tr_InterviewerBillingRate
   ON  [dbo].[InterviewerBillingRate]    
   AFTER  INSERT,UPDATE    
AS     
BEGIN    
  Declare @id int,@IRFId int,@InterviewerUserid int
  DECLARE @IDVar INT
  Select @id = Id from inserted
  Select @IRFId = IRFId,@InterviewerUserid=Userid from InterviewerBillingRate where id=@id    
    
 DECLARE @tbl TABLE (id INT);
 INSERT @tbl (id)
 Select im.id From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId
 join UserList ul on ul.EmailId=im.interviewer where ul.id=@InterviewerUserid and IRF.id=@IRFId and im.confirmationStatus=1 and Isnull(im.RedoInterview,0)=0
    
 DECLARE @RowCount INT = (SELECT COUNT(*) FROM @tbl);
 
 WHILE @RowCount > 0 BEGIN
  SELECT @IDVar=id FROM @tbl 
         ORDER BY ID DESC OFFSET @RowCount - 1 ROWS FETCH NEXT 1 ROWS ONLY;
  Exec usp_AddOrUpdateWalletDetails @IDVar,'System','InterviewBilling','Dr',0
  SET @RowCount -= 1;
 END
END
GO