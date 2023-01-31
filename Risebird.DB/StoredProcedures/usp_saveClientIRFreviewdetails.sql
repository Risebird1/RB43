IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_saveClientIRFreviewdetails'))
BEGIN
    DROP PROCEDURE dbo.usp_saveClientIRFreviewdetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_saveClientIRFreviewdetails
@emailId varchar(150),
@rating int,
@comments varchar(1000),
@IRFId varchar(15)
AS
BEGIN
Declare @id int
Select @id=id From IRF where IRFId=@IRFId
Insert into ClientIRFReviewDetails(ClientEmailid,Rating,Comment,IRFId,CreatedOn) values(@emailId,@rating,@comments,@id,getdate())
END
GO
