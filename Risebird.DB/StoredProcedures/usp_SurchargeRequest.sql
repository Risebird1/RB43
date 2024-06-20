IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SurchargeRequest'))
BEGIN
    DROP PROCEDURE dbo.usp_SurchargeRequest
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_SurchargeRequest
@InterviewId int,
@RequestBy varchar(150),
@RequestAt Datetime,
@RequestType varchar(100),
@Surcharge int,
@SurchargeComment varchar(500),
@IRFId int
As
Begin
Insert into SurchargeActivityTracker(InterviewId,Surcharge,SurchargeComment,RequestBy,RequestAt,RequestType,IRFId) 
values(@InterviewId,@Surcharge,@SurchargeComment,@RequestBy,@RequestAt,@RequestType,@IRFId)
End
GO