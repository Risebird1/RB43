-- ============================================================================
-- TEST DATA FOR MANUAL REVIEW & RESUME INGESTION FEATURE
-- Run this on development database only!
-- ============================================================================

-- Use a transaction so you can rollback if needed
BEGIN TRANSACTION;

PRINT 'Starting test data insertion...';

-- ============================================================================
-- 1. CREATE TEST CLIENT (or use existing if ID conflict)
-- ============================================================================
DECLARE @TestClientId INT;

-- Check if test client already exists
IF EXISTS (SELECT 1 FROM dbo.Client WHERE Name = 'TestCorp Technologies')
BEGIN
    SELECT @TestClientId = id FROM dbo.Client WHERE Name = 'TestCorp Technologies';
    PRINT 'Using existing test client ID: ' + CAST(@TestClientId AS VARCHAR);
END
ELSE
BEGIN
    INSERT INTO dbo.Client (
        [Name], [Description], [CompanyLink], [tenantId]
    ) VALUES (
        'TestCorp Technologies',
        'Test company for manual review feature testing',
        'https://testcorp.com',
        'tenant-test-001'
    );
    SET @TestClientId = SCOPE_IDENTITY();
    PRINT 'Created test client ID: ' + CAST(@TestClientId AS VARCHAR);
END

-- ============================================================================
-- 2. CREATE TEST HR USERS (Risebird HR)
-- ============================================================================
DECLARE @HRUserId1 INT, @HRUserId2 INT;

-- HR User 1
IF EXISTS (SELECT 1 FROM dbo.UserList WHERE EmailId = 'testhr1@risebird.io')
BEGIN
    SELECT @HRUserId1 = id FROM dbo.UserList WHERE EmailId = 'testhr1@risebird.io';
END
ELSE
BEGIN
    INSERT INTO dbo.UserList (idProfile, FirstName, LastName, EmailId, IsActive)
    VALUES (3, 'Test', 'HR One', 'testhr1@risebird.io', 1);
    SET @HRUserId1 = SCOPE_IDENTITY();
END
PRINT 'HR User 1 ID: ' + CAST(@HRUserId1 AS VARCHAR);

-- HR User 2
IF EXISTS (SELECT 1 FROM dbo.UserList WHERE EmailId = 'testhr2@risebird.io')
BEGIN
    SELECT @HRUserId2 = id FROM dbo.UserList WHERE EmailId = 'testhr2@risebird.io';
END
ELSE
BEGIN
    INSERT INTO dbo.UserList (idProfile, FirstName, LastName, EmailId, IsActive)
    VALUES (3, 'Test', 'HR Two', 'testhr2@risebird.io', 1);
    SET @HRUserId2 = SCOPE_IDENTITY();
END
PRINT 'HR User 2 ID: ' + CAST(@HRUserId2 AS VARCHAR);

-- ============================================================================
-- 3. CREATE TEST IRFS (Skill Channels)
-- ============================================================================
DECLARE @IRFId1 INT, @IRFId2 INT, @IRFId3 INT, @IRFId4 INT;

-- IRF 1: Java Developer
IF EXISTS (SELECT 1 FROM dbo.IRF WHERE IRFId = 'TESTJavaDv')
BEGIN
    SELECT @IRFId1 = id FROM dbo.IRF WHERE IRFId = 'TESTJavaDv';
END
ELSE
BEGIN
    INSERT INTO dbo.IRF (
        IRFId, ClientId, JDTiltle, SkillCluster, StartDate, EndDate, 
        RisebirdHr, Discount, IsBiometricEnabled, PriorityLevel, IsPaused, CreatedOn,
        JDLink, NoOfPosition, MeetingPlatform, IsDeleted
    ) VALUES (
        'TESTJavaDv', @TestClientId, 'Senior Java Developer', 'Java, Spring Boot, Microservices',
        DATEADD(MONTH, -1, GETDATE()), DATEADD(MONTH, 3, GETDATE()),
        @HRUserId1, 0, 0, 2, 0, GETDATE(),
        'https://example.com/jd/java-developer.pdf', 5, 'Zoho', 0
    );
    SET @IRFId1 = SCOPE_IDENTITY();
END
PRINT 'IRF 1 (Java) ID: ' + CAST(@IRFId1 AS VARCHAR);

-- IRF 2: Python Developer
IF EXISTS (SELECT 1 FROM dbo.IRF WHERE IRFId = 'TESTPythDv')
BEGIN
    SELECT @IRFId2 = id FROM dbo.IRF WHERE IRFId = 'TESTPythDv';
END
ELSE
BEGIN
    INSERT INTO dbo.IRF (
        IRFId, ClientId, JDTiltle, SkillCluster, StartDate, EndDate, 
        RisebirdHr, Discount, IsBiometricEnabled, PriorityLevel, IsPaused, CreatedOn,
        JDLink, NoOfPosition, MeetingPlatform, IsDeleted
    ) VALUES (
        'TESTPythDv', @TestClientId, 'Python Backend Engineer', 'Python, Django, FastAPI',
        DATEADD(MONTH, -1, GETDATE()), DATEADD(MONTH, 3, GETDATE()),
        @HRUserId1, 0, 0, 2, 0, GETDATE(),
        'https://example.com/jd/python-developer.pdf', 3, 'Zoho', 0
    );
    SET @IRFId2 = SCOPE_IDENTITY();
END
PRINT 'IRF 2 (Python) ID: ' + CAST(@IRFId2 AS VARCHAR);

-- IRF 3: React Developer
IF EXISTS (SELECT 1 FROM dbo.IRF WHERE IRFId = 'TESTReactD')
BEGIN
    SELECT @IRFId3 = id FROM dbo.IRF WHERE IRFId = 'TESTReactD';
END
ELSE
BEGIN
    INSERT INTO dbo.IRF (
        IRFId, ClientId, JDTiltle, SkillCluster, StartDate, EndDate, 
        RisebirdHr, Discount, IsBiometricEnabled, PriorityLevel, IsPaused, CreatedOn,
        JDLink, NoOfPosition, MeetingPlatform, IsDeleted
    ) VALUES (
        'TESTReactD', @TestClientId, 'React Frontend Developer', 'React, TypeScript, Redux',
        DATEADD(MONTH, -1, GETDATE()), DATEADD(MONTH, 3, GETDATE()),
        @HRUserId2, 0, 0, 2, 0, GETDATE(),
        'https://example.com/jd/react-developer.pdf', 4, 'Zoho', 0
    );
    SET @IRFId3 = SCOPE_IDENTITY();
END
PRINT 'IRF 3 (React) ID: ' + CAST(@IRFId3 AS VARCHAR);

-- IRF 4: DevOps Engineer
IF EXISTS (SELECT 1 FROM dbo.IRF WHERE IRFId = 'TESTDevOps')
BEGIN
    SELECT @IRFId4 = id FROM dbo.IRF WHERE IRFId = 'TESTDevOps';
END
ELSE
BEGIN
    INSERT INTO dbo.IRF (
        IRFId, ClientId, JDTiltle, SkillCluster, StartDate, EndDate, 
        RisebirdHr, Discount, IsBiometricEnabled, PriorityLevel, IsPaused, CreatedOn,
        JDLink, NoOfPosition, MeetingPlatform, IsDeleted
    ) VALUES (
        'TESTDevOps', @TestClientId, 'Senior DevOps Engineer', 'AWS, Kubernetes, Docker, CI/CD',
        DATEADD(MONTH, -1, GETDATE()), DATEADD(MONTH, 3, GETDATE()),
        @HRUserId2, 0, 0, 1, 0, GETDATE(),
        'https://example.com/jd/devops-engineer.pdf', 2, 'Zoho', 0
    );
    SET @IRFId4 = SCOPE_IDENTITY();
END
PRINT 'IRF 4 (DevOps) ID: ' + CAST(@IRFId4 AS VARCHAR);

-- ============================================================================
-- 4. CREATE TEST SUPPORT TICKETS
-- ============================================================================
DECLARE @TicketId1 INT, @TicketId2 INT, @TicketId3 INT, @TicketId4 INT;

-- Ticket 1: Multiple resumes, no scheduling data
INSERT INTO dbo.SupportTickets (
    SenderEmail, Subject, MailContent, ReceivedAt, Intent, PrimarySpecialization,
    MailBodyIntent, AttachmentJobDescription, AttachmentPdf, AttachmentResumes,
    DriveFolderLink, SummaryMail, ClientId, AssignedTo, Status, HasPendingResumes, JobTitle
) VALUES (
    'recruiter@testcorp.com',
    'New Candidate Submission - Java and Python Developers',
    'Hi Team,

Please find attached resumes of candidates for your open positions:

1. John Smith - Java Developer with 5+ years experience in Spring Boot and Microservices
2. Sarah Johnson - Python Developer with Django expertise  
3. Mike Chen - Full-stack developer with React and Node.js experience

All candidates are immediately available to join.

Best regards,
TestCorp Recruitment Team',
    DATEADD(HOUR, -2, GETDATE()),
    'New candidate submission',
    'Java, Python',
    'Resume submission for open positions',
    0, 0, 1,
    'https://drive.google.com/folder/test-folder-1',
    'Recruiter submitted 3 resumes for Java and Python positions. Candidates are immediately available.',
    @TestClientId,
    'testhr1@risebird.io',
    'Open',
    1,
    'Java Developer, Python Developer'
);
SET @TicketId1 = SCOPE_IDENTITY();
PRINT 'Ticket 1 ID: ' + CAST(@TicketId1 AS VARCHAR);

-- Ticket 2: Single resume WITH scheduling data
INSERT INTO dbo.SupportTickets (
    SenderEmail, Subject, MailContent, ReceivedAt, Intent, PrimarySpecialization,
    MailBodyIntent, AttachmentJobDescription, AttachmentPdf, AttachmentResumes,
    DriveFolderLink, SummaryMail, ClientId, AssignedTo, Status, HasPendingResumes, JobTitle
) VALUES (
    'hiring@testcorp.com',
    'Interview Request - React Developer with Available Slots',
    'Hello,

I would like to schedule an interview for the following candidate:

Candidate: Emily Davis
Position: React Frontend Developer
Email: emily.davis@email.com

Preferred Interview Slots:
- April 5, 2026 at 10:00 AM IST
- April 6, 2026 at 2:00 PM IST

Please let me know which slot works best.

Resume attached.

Thanks,
HR Team',
    DATEADD(HOUR, -1, GETDATE()),
    'New interview request',
    'React, Frontend',
    'Interview scheduling request with candidate availability',
    0, 0, 1,
    'https://drive.google.com/folder/test-folder-2',
    'Interview request for React developer with specific time slots provided.',
    @TestClientId,
    'testhr2@risebird.io',
    'Open',
    1,
    'React Developer'
);
SET @TicketId2 = SCOPE_IDENTITY();
PRINT 'Ticket 2 ID: ' + CAST(@TicketId2 AS VARCHAR);

-- Ticket 3: DevOps position, partial information
INSERT INTO dbo.SupportTickets (
    SenderEmail, Subject, MailContent, ReceivedAt, Intent, PrimarySpecialization,
    MailBodyIntent, AttachmentJobDescription, AttachmentPdf, AttachmentResumes,
    DriveFolderLink, SummaryMail, ClientId, AssignedTo, Status, HasPendingResumes, JobTitle
) VALUES (
    'talent@testcorp.com',
    'Resume Submission - DevOps Engineer',
    'Hi,

Please review the attached resume for the DevOps position.

Candidate has strong experience with:
- AWS (EC2, S3, Lambda, EKS)
- Kubernetes and Docker
- Terraform and Ansible
- CI/CD pipelines (Jenkins, GitHub Actions)

Let me know if you need any additional information.

Regards',
    DATEADD(MINUTE, -30, GETDATE()),
    'New candidate submission',
    'DevOps, AWS, Kubernetes',
    'Resume for DevOps position',
    0, 0, 1,
    'https://drive.google.com/folder/test-folder-3',
    'Single resume submission for DevOps engineer role. Strong cloud and container skills.',
    @TestClientId,
    'testhr2@risebird.io',
    'Open',
    1,
    'DevOps Engineer'
);
SET @TicketId3 = SCOPE_IDENTITY();
PRINT 'Ticket 3 ID: ' + CAST(@TicketId3 AS VARCHAR);

-- Ticket 4: Multiple candidates with scheduling data
INSERT INTO dbo.SupportTickets (
    SenderEmail, Subject, MailContent, ReceivedAt, Intent, PrimarySpecialization,
    MailBodyIntent, AttachmentJobDescription, AttachmentPdf, AttachmentResumes,
    DriveFolderLink, SummaryMail, ClientId, AssignedTo, Status, HasPendingResumes, JobTitle
) VALUES (
    'recruiter@testcorp.com',
    'Urgent: Schedule Interviews for Java Developers',
    'Hi Team,

We have 2 strong Java candidates ready for interviews:

1. Alex Kumar
   Email: alex.kumar@gmail.com
   Available: April 7, 2026 at 11:00 AM IST
   
2. Priya Sharma  
   Email: priya.sharma@outlook.com
   Available: April 8, 2026 at 3:00 PM IST

Both candidates are excited about the opportunity. Please schedule at the earliest.

Resumes attached.

Thanks!',
    DATEADD(MINUTE, -15, GETDATE()),
    'New interview request',
    'Java, Spring Boot',
    'Urgent interview scheduling for Java developers',
    0, 0, 1,
    'https://drive.google.com/folder/test-folder-4',
    'Two Java developer candidates with specific interview slots. Urgent scheduling needed.',
    @TestClientId,
    'testhr1@risebird.io',
    'Open',
    1,
    'Java Developer'
);
SET @TicketId4 = SCOPE_IDENTITY();
PRINT 'Ticket 4 ID: ' + CAST(@TicketId4 AS VARCHAR);

-- ============================================================================
-- 5. CREATE PENDING RESUMES FOR EACH TICKET
-- ============================================================================

-- Ticket 1 Pending Resumes (3 resumes, no scheduling)
INSERT INTO dbo.SupportTicketPendingResumes (
    SupportTicketId, ResumeUrl, CandidateName, CandidateEmail,
    ResolvedIRFId, ResolvedIRFStringId, ResolvedJobTitle, ResolvedHREmail,
    OriginalResolvedIRFId, OriginalResolvedIRFStringId,
    HasSchedulingData, Status
) VALUES 
(
    @TicketId1,
    'https://drive.google.com/file/d/abc123/view',
    'John Smith',
    'john.smith@email.com',
    @IRFId1, 'TESTJavaDv', 'Senior Java Developer', 'testhr1@risebird.io',
    @IRFId1, 'TESTJavaDv',
    0, 'Pending'
),
(
    @TicketId1,
    'https://drive.google.com/file/d/def456/view',
    'Sarah Johnson',
    'sarah.j@email.com',
    @IRFId2, 'TESTPythDv', 'Python Backend Engineer', 'testhr1@risebird.io',
    @IRFId2, 'TESTPythDv',
    0, 'Pending'
),
(
    @TicketId1,
    'https://drive.google.com/file/d/ghi789/view',
    'Mike Chen',
    NULL,  -- No email provided
    @IRFId3, 'TESTReactD', 'React Frontend Developer', 'testhr2@risebird.io',
    @IRFId3, 'TESTReactD',
    0, 'Pending'
);
PRINT 'Created 3 pending resumes for Ticket 1';

-- Ticket 2 Pending Resume (1 resume WITH scheduling)
INSERT INTO dbo.SupportTicketPendingResumes (
    SupportTicketId, ResumeUrl, CandidateName, CandidateEmail,
    ResolvedIRFId, ResolvedIRFStringId, ResolvedJobTitle, ResolvedHREmail,
    OriginalResolvedIRFId, OriginalResolvedIRFStringId,
    HasSchedulingData, ScheduledInterviewDateTime, ScheduleTimezone, Status
) VALUES (
    @TicketId2,
    'https://drive.google.com/file/d/jkl012/view',
    'Emily Davis',
    'emily.davis@email.com',
    @IRFId3, 'TESTReactD', 'React Frontend Developer', 'testhr2@risebird.io',
    @IRFId3, 'TESTReactD',
    1, '2026-04-05 10:00:00', 'IST', 'Pending'
);
PRINT 'Created 1 pending resume for Ticket 2 (with scheduling)';

-- Ticket 3 Pending Resume (1 resume, no scheduling, no email)
INSERT INTO dbo.SupportTicketPendingResumes (
    SupportTicketId, ResumeUrl, CandidateName, CandidateEmail,
    ResolvedIRFId, ResolvedIRFStringId, ResolvedJobTitle, ResolvedHREmail,
    OriginalResolvedIRFId, OriginalResolvedIRFStringId,
    HasSchedulingData, Status
) VALUES (
    @TicketId3,
    'https://drive.google.com/file/d/mno345/view',
    'Anonymous Candidate',
    NULL,
    @IRFId4, 'TESTDevOps', 'Senior DevOps Engineer', 'testhr2@risebird.io',
    @IRFId4, 'TESTDevOps',
    0, 'Pending'
);
PRINT 'Created 1 pending resume for Ticket 3';

-- Ticket 4 Pending Resumes (2 resumes WITH scheduling)
INSERT INTO dbo.SupportTicketPendingResumes (
    SupportTicketId, ResumeUrl, CandidateName, CandidateEmail,
    ResolvedIRFId, ResolvedIRFStringId, ResolvedJobTitle, ResolvedHREmail,
    OriginalResolvedIRFId, OriginalResolvedIRFStringId,
    HasSchedulingData, ScheduledInterviewDateTime, ScheduleTimezone, Status
) VALUES 
(
    @TicketId4,
    'https://drive.google.com/file/d/pqr678/view',
    'Alex Kumar',
    'alex.kumar@gmail.com',
    @IRFId1, 'TESTJavaDv', 'Senior Java Developer', 'testhr1@risebird.io',
    @IRFId1, 'TESTJavaDv',
    1, '2026-04-07 11:00:00', 'IST', 'Pending'
),
(
    @TicketId4,
    'https://drive.google.com/file/d/stu901/view',
    'Priya Sharma',
    'priya.sharma@outlook.com',
    @IRFId1, 'TESTJavaDv', 'Senior Java Developer', 'testhr1@risebird.io',
    @IRFId1, 'TESTJavaDv',
    1, '2026-04-08 15:00:00', 'IST', 'Pending'
);
PRINT 'Created 2 pending resumes for Ticket 4 (with scheduling)';

-- ============================================================================
-- 6. CREATE CLIENT USER (to test IRF dropdown)
-- ============================================================================
DECLARE @ClientUserId INT;

IF EXISTS (SELECT 1 FROM dbo.UserList WHERE EmailId = 'recruiter@testcorp.com')
BEGIN
    SELECT @ClientUserId = id FROM dbo.UserList WHERE EmailId = 'recruiter@testcorp.com';
END
ELSE
BEGIN
    INSERT INTO dbo.UserList (idProfile, FirstName, LastName, EmailId, IsActive)
    VALUES (4, 'Test', 'Recruiter', 'recruiter@testcorp.com', 1);
    SET @ClientUserId = SCOPE_IDENTITY();
END

-- Link to ClientUser if not exists
IF NOT EXISTS (SELECT 1 FROM dbo.ClientUser WHERE UserId = @ClientUserId)
BEGIN
    INSERT INTO dbo.ClientUser (UserId, ClientId, RoleId, [Description])
    VALUES (@ClientUserId, @TestClientId, 1, 1);  -- RoleId=1, Description=1 (placeholder values)
    PRINT 'Created ClientUser mapping';
END

-- ============================================================================
-- SUMMARY
-- ============================================================================
PRINT '';
PRINT '============================================';
PRINT 'TEST DATA CREATED SUCCESSFULLY!';
PRINT '============================================';
PRINT 'Client: TestCorp Technologies (ID: ' + CAST(@TestClientId AS VARCHAR) + ')';
PRINT '';
PRINT 'IRFs Created:';
PRINT '  - TESTJavaDv: Senior Java Developer (ID: ' + CAST(@IRFId1 AS VARCHAR) + ')';
PRINT '  - TESTPythDv: Python Backend Engineer (ID: ' + CAST(@IRFId2 AS VARCHAR) + ')';
PRINT '  - TESTReactD: React Frontend Developer (ID: ' + CAST(@IRFId3 AS VARCHAR) + ')';
PRINT '  - TESTDevOps: Senior DevOps Engineer (ID: ' + CAST(@IRFId4 AS VARCHAR) + ')';
PRINT '';
PRINT 'Support Tickets with Pending Resumes:';
PRINT '  - Ticket ' + CAST(@TicketId1 AS VARCHAR) + ': 3 resumes (no scheduling)';
PRINT '  - Ticket ' + CAST(@TicketId2 AS VARCHAR) + ': 1 resume (WITH scheduling)';
PRINT '  - Ticket ' + CAST(@TicketId3 AS VARCHAR) + ': 1 resume (no email)';
PRINT '  - Ticket ' + CAST(@TicketId4 AS VARCHAR) + ': 2 resumes (WITH scheduling)';
PRINT '';
PRINT 'Total Pending Resumes: 7';
PRINT '============================================';

-- Verify the data
SELECT 'SupportTickets with Pending Resumes' AS [Table], COUNT(*) AS [Count] 
FROM dbo.SupportTickets WHERE HasPendingResumes = 1 AND Id IN (@TicketId1, @TicketId2, @TicketId3, @TicketId4)
UNION ALL
SELECT 'Pending Resumes', COUNT(*) 
FROM dbo.SupportTicketPendingResumes WHERE SupportTicketId IN (@TicketId1, @TicketId2, @TicketId3, @TicketId4);

-- Show the pending resumes
SELECT 
    pr.Id AS PendingResumeId,
    pr.SupportTicketId AS TicketId,
    pr.CandidateName,
    pr.CandidateEmail,
    pr.ResolvedIRFStringId AS IRFId,
    pr.ResolvedJobTitle,
    CASE WHEN pr.HasSchedulingData = 1 THEN 'Yes' ELSE 'No' END AS HasScheduling,
    pr.ScheduledInterviewDateTime,
    pr.Status
FROM dbo.SupportTicketPendingResumes pr
WHERE pr.SupportTicketId IN (@TicketId1, @TicketId2, @TicketId3, @TicketId4)
ORDER BY pr.SupportTicketId, pr.Id;

COMMIT TRANSACTION;
PRINT '';
PRINT 'Transaction committed. Test data is now in the database.';
GO


-- ============================================================================
-- CLEANUP SCRIPT (Run this to remove test data if needed)
-- ============================================================================
/*
-- CAUTION: Uncomment and run this to remove all test data

BEGIN TRANSACTION;

-- Delete pending resumes first (FK constraint)
DELETE FROM dbo.SupportTicketPendingResumes 
WHERE SupportTicketId IN (
    SELECT Id FROM dbo.SupportTickets WHERE SenderEmail IN ('recruiter@testcorp.com', 'hiring@testcorp.com', 'talent@testcorp.com')
);

-- Delete tickets
DELETE FROM dbo.SupportTickets 
WHERE SenderEmail IN ('recruiter@testcorp.com', 'hiring@testcorp.com', 'talent@testcorp.com');

-- Delete IRFs
DELETE FROM dbo.IRF WHERE IRFId IN ('TESTJavaDv', 'TESTPythDv', 'TESTReactD', 'TESTDevOps');

-- Delete client user mapping
DELETE FROM dbo.ClientUser WHERE UserId IN (SELECT id FROM dbo.UserList WHERE EmailId = 'recruiter@testcorp.com');

-- Delete test users
DELETE FROM dbo.UserList WHERE EmailId IN ('testhr1@risebird.io', 'testhr2@risebird.io', 'recruiter@testcorp.com');

-- Delete client
DELETE FROM dbo.Client WHERE Name = 'TestCorp Technologies';

COMMIT TRANSACTION;
PRINT 'Test data cleaned up successfully.';
*/
