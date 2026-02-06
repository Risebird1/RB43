-- Table to store typing speed sample texts
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TypingSpeedSampleTexts')
BEGIN  
CREATE TABLE [dbo].[TypingSpeedSampleTexts](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SampleText] [nvarchar](max) NULL,
	[IsActive] [bit] NULL DEFAULT 1,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
	[UpdatedAt] [datetime] NULL,
	[UpdatedBy] [varchar](150) NULL,
 CONSTRAINT [PK_TypingSpeedSampleTexts] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN
	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'IsActive')
		  AND Object_ID = Object_ID(N'TypingSpeedSampleTexts'))
	BEGIN
		ALTER TABLE TypingSpeedSampleTexts
		Add IsActive bit NULL DEFAULT 1
	END

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'UpdatedAt')
		  AND Object_ID = Object_ID(N'TypingSpeedSampleTexts'))
	BEGIN
		ALTER TABLE TypingSpeedSampleTexts
		Add UpdatedAt datetime NULL
	END

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'UpdatedBy')
		  AND Object_ID = Object_ID(N'TypingSpeedSampleTexts'))
	BEGIN
		ALTER TABLE TypingSpeedSampleTexts
		Add UpdatedBy varchar(150) NULL
	END
END
GO

-- Insert default sample texts if table is empty
IF NOT EXISTS (SELECT 1 FROM TypingSpeedSampleTexts)
BEGIN
	INSERT INTO TypingSpeedSampleTexts (SampleText, IsActive, CreatedAt, CreatedBy)
	VALUES 
	('The quick brown fox jumps over the lazy dog. Programming requires attention to detail and careful consideration of logic. Software development is both an art and a science that requires constant learning and adaptation.', 1, GETDATE(), 'System'),
	('In the world of technology, innovation drives progress forward. Developers must balance creativity with practicality while building solutions that meet user needs. Effective communication and teamwork are essential skills in modern software development.', 1, GETDATE(), 'System'),
	('Artificial intelligence and machine learning are transforming industries across the globe. These technologies enable computers to learn from data and make intelligent decisions. The future of computing lies in creating systems that can think and adapt like humans.', 1, GETDATE(), 'System'),
	('Web development encompasses a wide range of technologies and frameworks. From frontend design to backend architecture, developers must master multiple disciplines. Building responsive and accessible applications requires dedication and continuous improvement.', 1, GETDATE(), 'System'),
	('Data structures and algorithms form the foundation of computer science. Understanding how to efficiently store and manipulate data is crucial for building scalable systems. Problem-solving skills are developed through practice and perseverance.', 1, GETDATE(), 'System');
END
GO
