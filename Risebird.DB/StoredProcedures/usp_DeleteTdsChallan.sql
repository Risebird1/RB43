CREATE OR ALTER PROCEDURE [dbo].[usp_DeleteTdsChallan]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    -- TdsChallanInterviewer rows cascade-delete via FK ON DELETE CASCADE
    DELETE FROM TdsChallan WHERE id = @id;
END
GO
