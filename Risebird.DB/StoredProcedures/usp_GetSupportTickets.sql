-- usp_GetSupportTickets
-- Returns all support tickets joined with Client.Name for the dashboard view.
-- Ordered most-recent first.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetSupportTickets]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        st.Id,
        st.SenderEmail,
        st.Subject,
        st.MailContent,
        st.ReceivedAt,
        st.Intent,
        st.UserId,
        st.ClientId,
        st.Status,
        st.CreatedAt,
        c.[Name]  AS ClientName
    FROM  dbo.SupportTickets st
    LEFT  JOIN dbo.Client     c  ON c.id  = st.ClientId
    ORDER BY st.ReceivedAt DESC;
END
GO
