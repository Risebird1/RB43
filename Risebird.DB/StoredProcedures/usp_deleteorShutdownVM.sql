IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_deleteorShutdownVM'))
BEGIN
    DROP PROCEDURE dbo.usp_deleteorShutdownVM
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_deleteorShutdownVM
As
BEGIN
Select ResourceGroupName,Name,'Delete' methodtype from VMProperties 
where DATEDIFF(minute,'2023-01-16 12:45:50.537',Getdate())>(isnull(deletetime,0)*60) and isnull(deletetime,0)>0
union
Select ResourceGroupName,Name,'Shutdown' methodtype from VMProperties 
where DATEDIFF(minute,'2023-01-16 12:45:50.537',Getdate())>(isnull(shutdowntime,0)*60) and isnull(shutdowntime,0)>0
END
GO

