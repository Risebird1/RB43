IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'tempCandidate_TableType')
BEGIN

CREATE TYPE tempCandidate_TableType AS TABLE  
(  
   id int
)  
End