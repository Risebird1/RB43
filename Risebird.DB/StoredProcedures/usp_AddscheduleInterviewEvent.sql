IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddscheduleInterviewEvent'))
BEGIN
    DROP PROCEDURE dbo.usp_AddscheduleInterviewEvent
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_AddscheduleInterviewEvent    
@id int,    
@EventName varchar(150),    
@SkillChannel varchar(50),    
@StartDate datetime,    
@EndDate datetime,    
@duration int,    
@Uniqueid varchar(50),
@Concurrency int,
@BufferTime int,
@TimeSlotDays int,
@sourcevalue varchar(50)
AS    
BEGIN    
Declare @Count int=0
Select @Count=Count(id) From ScheduleInterviewEvent where SkillChannel=@SkillChannel
if(@Count>0)    
BEGIN    
Update ScheduleInterviewEvent set SkillChannel=@SkillChannel,StartDate=@StartDate,EndDate=@EndDate,Concurrency=@Concurrency,BufferTime=@BufferTime,TimeSlotDays=@TimeSlotDays where SkillChannel=@SkillChannel
Select @id    
END    
ELSE    
BEGIN    
Insert into ScheduleInterviewEvent(EventName,SkillChannel,StartDate,EndDate,duration,Uniqueid,IsActive,Concurrency,BufferTime,TimeSlotDays,sourcevalue)    
values(@EventName,@SkillChannel,@StartDate,@EndDate,@duration,@Uniqueid,1,@Concurrency,@BufferTime,@TimeSlotDays,@sourcevalue)    
    
Select SCOPE_IDENTITY()    
END    
END 

GO