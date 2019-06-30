IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_GetMenuBasedOnEmailId'))
BEGIN
    DROP PROCEDURE dbo.sp_GetMenuBasedOnEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_GetMenuBasedOnEmailId] 
  @EmailId nvarchar(300)   
AS
BEGIN
select fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder from [dbo].[Function] fn
join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction
join [dbo].[Profile] pl on pl.id = pf.idprofile
join [dbo].[UserList] ul on pl.id = ul.idprofile
where ul.EmailId LIKE @EmailId AND ul.IsActive=1 

END
GO 




