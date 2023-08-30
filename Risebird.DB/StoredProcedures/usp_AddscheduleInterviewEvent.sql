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
@TimeSlotDays int
AS    
BEGIN    
if(@id>0)    
BEGIN    
Update ScheduleInterviewEvent set SkillChannel=@SkillChannel,StartDate=@StartDate,EndDate=@EndDate,Concurrency=@Concurrency,BufferTime=@BufferTime,TimeSlotDays=@TimeSlotDays where id=@id    
Select @id    
END    
ELSE    
BEGIN    
Insert into ScheduleInterviewEvent(EventName,SkillChannel,StartDate,EndDate,duration,Uniqueid,IsActive,Concurrency,BufferTime,TimeSlotDays)    
values(@EventName,@SkillChannel,@StartDate,@EndDate,@duration,@Uniqueid,1,@Concurrency,@BufferTime,@TimeSlotDays)    
    
Select SCOPE_IDENTITY()    
END    
END  

GO