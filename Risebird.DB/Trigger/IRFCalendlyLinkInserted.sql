-- ==============================================
-- Create dml trigger template Azure SQL Database 
-- ==============================================
-- Drop the dml trigger if it already exists
IF EXISTS(
  SELECT *
    FROM sys.triggers
   WHERE name = N'IRFCalendlyLinkInserted'
     AND parent_class_desc = N'OBJECT_OR_COLUMN'
)
	DROP TRIGGER IRFCalendlyLinkInserted
GO

CREATE TRIGGER IRFCalendlyLinkInserted
   ON  IRF
   After INSERT
AS 
BEGIN
	
	SET NOCOUNT ON;
	Declare @IRFId int, @CalendlyLink varchar(500)
	
	SELECT @IRFId = ins.id FROM INSERTED ins
	SELECT @CalendlyLink = ins.CalendlyLink FROM INSERTED ins

	if(@CalendlyLink is not null and @CalendlyLink != '')
	begin
		insert into TransCalendlyLinkAddedUpdated values (@IRFId, getdate())
	end

	
    

END
GO



