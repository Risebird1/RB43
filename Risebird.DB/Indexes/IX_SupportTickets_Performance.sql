-- Performance indexes for SupportTickets dashboard
-- Run this script to improve pagination performance with large datasets

-- Index for filtering and sorting
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupportTickets_ReceivedAt_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SupportTickets_ReceivedAt_Status] 
    ON [dbo].[SupportTickets] ([ReceivedAt] DESC, [Status])
    INCLUDE ([SenderEmail], [Subject], [Intent], [PrimarySpecialization], [AssignedTo], [ClientId], [HasPendingResumes]);
END
GO

-- Index for AssignedTo filtering (System tab exclusion)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupportTickets_AssignedTo')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SupportTickets_AssignedTo] 
    ON [dbo].[SupportTickets] ([AssignedTo])
    INCLUDE ([Id], [ReceivedAt]);
END
GO

-- Index for SupportTicketHistory EXISTS check
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupportTicketHistory_TicketId_EventType')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SupportTicketHistory_TicketId_EventType] 
    ON [dbo].[SupportTicketHistory] ([TicketId], [EventType], [ToStatus]);
END
GO

-- Index for pending resumes lookup
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupportTicketPendingResumes_TicketId_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SupportTicketPendingResumes_TicketId_Status] 
    ON [dbo].[SupportTicketPendingResumes] ([SupportTicketId], [Status]);
END
GO

PRINT 'Performance indexes created successfully.';
GO
