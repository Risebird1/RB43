IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_EventTimeSlotsBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_EventTimeSlotsBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_EventTimeSlotsBulkInsert]    
(    
      @EventTimeslotBulkData AS [dbo].[EventTimeSlots_TableType] READONLY    
)    
AS    
BEGIN    
    
SET NOCOUNT ON;    
  Delete from ScheduleEventSlots where Eventid in (Select Eventid from @EventTimeslotBulkData)  
--This row performs bulk copy of object DataTable to Table in SQL    
INSERT INTO [dbo].[ScheduleEventSlots](    
 [Daytype],  
[Start],  
[End],  
[dayCheck],  
[Eventid])    
SELECT * FROM @EventTimeslotBulkData    
    
    
END 
GO