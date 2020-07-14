/****** Object:  StoredProcedure [dbo].[usp_AddUpdateClientFeedback]    Script Date: 23-09-2019 3.59.51 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateClientFeedback'))
BEGIN
    DROP PROCEDURE usp_AddUpdateClientFeedback
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_AddUpdateClientFeedback]
@id int,
@IsApproved int,
@IsSelected int,
@OfferStatus int,
@JoiningStatus int,
@ApprovalComments varchar(max),
@FinalComments varchar(max),
@JoiningComments varchar(max),
@OfferComments varchar(max),
@movetoCP bit


AS
Begin

IF NOT EXISTS (SELECT 1   FROM   InterviewComments    WHERE  InterviewId = @id)
Begin
INSERT INTO [dbo].[InterviewComments]
           ([InterviewId]
		   ,[IsApproved]
           ,[IsSelected]
		   ,[OfferStatus]
           ,[JoiningStatus]
           ,[ApprovalComments]
           ,[FinalComments]
           ,[JoiningComments]
           ,[OfferComments]
            ,[MoveToCP]
           )
     VALUES
           (@id,
@IsApproved,
@IsSelected,
@OfferStatus,
@JoiningStatus,
@ApprovalComments,
@FinalComments,
@JoiningComments,
@OfferComments,
@movetoCP
)

End

Else

Begin
Update InterviewComments set 
IsApproved =@IsApproved,
IsSelected = @IsSelected,
JoiningStatus =@JoiningStatus,
OfferStatus=@OfferStatus,
ApprovalComments =@ApprovalComments,
FinalComments =@FinalComments,
OfferComments =@OfferComments,
JoiningComments =@JoiningComments,
MoveToCP = @movetoCP
where InterviewId =@id


End

END

GO


