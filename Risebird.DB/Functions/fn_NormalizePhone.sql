IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_NormalizePhone]')
                      AND type IN (N'IF', N'TF', N'FN'))
BEGIN
    DROP FUNCTION dbo.fn_NormalizePhone
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Call opt-out: last 10 digits of any phone string, so '+91 98765-43210', '919876543210' and
-- '9876543210' compare equal. Used against Candidate.Phone (free text) and
-- RIGHT(CAST(UserList.MobileNumber AS varchar(20)), 10) (bigint). Returns NULL when there are no digits.
CREATE FUNCTION [dbo].[fn_NormalizePhone]
(
    @Phone nvarchar(200)
)
RETURNS varchar(10)
WITH SCHEMABINDING
AS
BEGIN
    IF @Phone IS NULL RETURN NULL

    DECLARE @Digits varchar(200) = ''
    DECLARE @i int = 1, @len int = LEN(@Phone), @c nchar(1)

    WHILE @i <= @len
    BEGIN
        SET @c = SUBSTRING(@Phone, @i, 1)
        IF @c LIKE N'[0123456789]' COLLATE Latin1_General_BIN SET @Digits = @Digits + CAST(@c AS varchar(1))
        SET @i = @i + 1
    END

    IF @Digits = '' RETURN NULL
    RETURN RIGHT(@Digits, 10)
END
GO
