IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_saveClientreviewdetails'))
BEGIN
    DROP PROCEDURE dbo.usp_saveClientreviewdetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
Create Procedure usp_saveClientreviewdetails
@emailId varchar(150),
@rating int,
@comments varchar(1000)
AS
BEGIN
Insert into ClientReviewDetails(ClientEmailid,Rating,Comment,CreatedOn) values(@emailId,@rating,@comments,getdate())
END
GO