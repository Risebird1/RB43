
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_HasEmailDroppedOrBounced]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_HasEmailDroppedOrBounced
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_HasEmailDroppedOrBounced(@emailId varchar(100))
Returns bit
as
Begin


declare @hasEmailDroppedOrBounced bit

if exists(select top 1 id from SendgridEventData sed
where ([event] like '%bounce%' or [event] like '%drop%') and email = @emailId )
begin
	set @hasEmailDroppedOrBounced = 1
end
else
begin
	set @hasEmailDroppedOrBounced=0
end

return @hasEmailDroppedOrBounced

end





