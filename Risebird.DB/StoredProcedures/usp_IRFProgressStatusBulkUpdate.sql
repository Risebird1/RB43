/****** Object:  StoredProcedure [dbo].[usp_IRFProgressStatusBulkUpdate]    Script Date: 29-07-2020 10.49.09 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_IRFProgressStatusBulkUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_IRFProgressStatusBulkUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


  
  
CREATE PROCEDURE [dbo].[usp_IRFProgressStatusBulkUpdate]  
(  
      @ProgressBulkData AS [dbo].[IRFActivityProgressStatus_TableType] READONLY  
)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  
--This row performs bulk copy of object DataTable to Table in SQL  
  
UPDATE IRF  
      SET idProgress = c2.idProgress  
      ,Comment = c2.Comment  
      FROM IRF c1  
      INNER JOIN @ProgressBulkData c2  
      ON c1.id = c2.id  
  
Create Table #temp(
id int,
Comment varchar(250),
idProgress int,
Userid varchar(200),
Status varchar(50)
)
Insert into #temp select * from @ProgressBulkData
Declare @count int
Declare @IRfid int
Declare @idProgress int
Declare @Countvalue int
Declare @Userid int
Declare @UserEmail varchar(200)
Select @count=Count(*) from #temp
while(@count>0)
Begin
set @count=@count-1;
Select top(1) @IRfid=id,@idProgress=idProgress,@UserEmail=Userid from #temp 
Select @Countvalue=Count(*) from IRFActivityStatus where IRFId=@IRfid and idProgress=@idProgress
Select @Userid=id from Userlist where EmailId=@UserEmail
if(@Countvalue=0)
Begin
Insert into IRFActivityStatus Select idProgress,@Userid,id as IRFId,Comment,Status from #temp where id=@IRfid and idProgress=@idProgress
END
ELSE
BEGIN
Update IRFActivityStatus set Status=c2.Status, Comment=c2.Comment,Userid=@Userid
      FROM IRFActivityStatus c1  
      INNER JOIN #temp c2  
      ON c1.IRFid = c2.id and  c1.idProgress=c2.idProgress
END

End

END


GO


