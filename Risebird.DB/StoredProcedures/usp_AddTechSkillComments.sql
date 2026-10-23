/****** Object:  StoredProcedure [dbo].[usp_AddTechSkillComments]    Script Date: 12/22/2021 5:45:42 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTechSkillComments'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTechSkillComments
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  

Create PROCEDURE [dbo].[usp_AddTechSkillComments]    
(    
   @TechSkillCommentsBulkData AS [dbo].[TechSkillComments_TableTypeV2] READONLY,--TechSkillComments_TableType  
   @IRFId int,
   @userId int
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran 
  
  -- Code sessions: this save deletes and re-inserts the rows, which would drop the IsCodingQuestion flag
  -- an interviewer set from the interview alignment page and orphan the sessions minted against the old
  -- ids. Remember the coding rows (keyed by owner and normalised question text) so both can be carried
  -- over to the re-inserted rows below. Nothing else about this save changes.
  declare @CodingRows table (OldId int, UserId int, CommentKey varchar(max), IsCodingQuestion bit, CodingTimeLimitInMinutes int)
  insert into @CodingRows (OldId, UserId, CommentKey, IsCodingQuestion, CodingTimeLimitInMinutes)
  select tsc.id, tsc.UserId,
         LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(tsc.Comment, CHAR(13), ''), CHAR(10), ''), CHAR(9), ''))),
         tsc.IsCodingQuestion, tsc.CodingTimeLimitInMinutes
  from TechSkillComments tsc
  where tsc.IRFId = @IRFId and ISNULL(tsc.IsCodingQuestion, 0) = 1

  declare @profileId int
  set @profileId = (select idProfile from UserList where id = @userId)
  if(@profileId = 1)--Admin
  begin
	delete from TechSkillComments where IRFId =@IRFId

   INSERT INTO [dbo].[TechSkillComments](    
    [IRFId] ,    
    [Skill],    
    [Comment],  
    [UserId],
	[SkillName])    
     
    
   SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.UserId, tscbd.SkillName FROM @TechSkillCommentsBulkData tscbd

  end
  else
  begin
	delete from TechSkillComments where IRFId =@IRFId and UserId =@userId

   INSERT INTO [dbo].[TechSkillComments](    
    [IRFId] ,    
    [Skill],    
    [Comment],  
    [UserId],
	[SkillName])    
     
    
   SELECT tscbd.IRFId, tscbd.Skill, LTRIM(RTRIM(
        REPLACE(
            REPLACE(
                REPLACE(tscbd.Comment, CHAR(13), ''),  -- \r
            CHAR(10), ''),                             -- \n
        CHAR(9), '')                                   -- \t
    )) AS Comment, tscbd.UserId, tscbd.SkillName FROM @TechSkillCommentsBulkData tscbd
  end

  if exists (select 1 from @CodingRows)
  begin
	-- Carry the coding flag onto the re-inserted row with the same owner and question text.
	update t
	   set IsCodingQuestion = c.IsCodingQuestion,
	       CodingTimeLimitInMinutes = c.CodingTimeLimitInMinutes
	  from TechSkillComments t
	  join @CodingRows c
	    on ISNULL(c.UserId, -1) = ISNULL(t.UserId, -1)
	   and c.CommentKey = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(t.Comment, CHAR(13), ''), CHAR(10), ''), CHAR(9), '')))
	 where t.IRFId = @IRFId
	   and ISNULL(t.IsCodingQuestion, 0) = 0

	-- Re-point sessions whose question row was just deleted at its re-inserted copy. One session per
	-- (interview, question) is kept unique: a session is only moved when the target is free.
	;with Remap as (
		select c.OldId, MIN(t.id) as NewId
		  from @CodingRows c
		  join TechSkillComments t
		    on t.IRFId = @IRFId
		   and ISNULL(c.UserId, -1) = ISNULL(t.UserId, -1)
		   and c.CommentKey = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(t.Comment, CHAR(13), ''), CHAR(10), ''), CHAR(9), '')))
		 where not exists (select 1 from TechSkillComments x where x.id = c.OldId)
		 group by c.OldId
	), Moves as (
		select ics.Id, r.NewId,
		       ROW_NUMBER() over (partition by ics.InterviewId, r.NewId order by ics.Id) as rn
		  from InterviewCodeSessions ics
		  join Remap r on r.OldId = ics.QuestionId
		 where not exists (select 1 from InterviewCodeSessions y
		                    where y.InterviewId = ics.InterviewId and y.QuestionId = r.NewId)
	)
	update ics
	   set QuestionId = m.NewId
	  from InterviewCodeSessions ics
	  join Moves m on m.Id = ics.Id
	 where m.rn = 1
  end


  
  commit tran  
 end try  
 begin catch  
  
  rollback transaction  
  
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
   @Severity int = ERROR_SEVERITY(),  
   @State smallint = ERROR_STATE()  
   
  RAISERROR (@Message, @Severity, @State)  
   
 end catch  
  
    
END    
GO


