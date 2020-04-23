/****** Object:  StoredProcedure [dbo].[usp_AddinterviewView]    Script Date: 4/18/2020 10:52:28 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddinterviewView'))
BEGIN
    DROP PROCEDURE dbo.usp_AddinterviewView
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[usp_AddinterviewView]
@Interviewid int
AS
BEGIN
DECLARE @ViewCount int
Select @ViewCount=TotalView from InterviewComments where InterviewId=@Interviewid
Update InterviewComments set TotalView=((IsNull(@ViewCount,0))+1) where InterviewId=@Interviewid

END