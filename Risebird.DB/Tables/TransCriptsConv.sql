
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransCriptsConv')
BEGIN
CREATE TABLE TransCriptsConv (
    Id INT  IDENTITY(1,1) not null,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(20),
    Message NVARCHAR(MAX),
    Timestamp DATETIME NOT NULL,
    DeliveryHeadQuestionId int null,
 CONSTRAINT [PK_TransCriptsConv] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
End

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DeliveryHeadQuestionId')
      AND Object_ID = Object_ID(N'TransCriptsConv'))
BEGIN
    ALTER TABLE TransCriptsConv
    Add DeliveryHeadQuestionId int null
END
Go