IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AssessmentGroupId' AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
    ALTER TABLE InterviewerDirectApplication ADD [AssessmentGroupId] INT NULL
END
GO
