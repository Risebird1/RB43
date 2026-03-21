-- usp_GetSupportTickets
-- Paginated, filtered support tickets joined with Client.Name.
-- TotalCount via window function avoids a second round-trip.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetSupportTickets]
    @Status      VARCHAR(50)    = NULL,
    @Intent      NVARCHAR(500)  = NULL,
    @SenderEmail VARCHAR(150)   = NULL,
    @DateFrom    DATETIME       = NULL,
    @DateTo      DATETIME       = NULL,
    @Page        INT            = 1,
    @PageSize    INT            = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page     < 1   SET @Page     = 1;
    IF @PageSize < 1   SET @PageSize = 25;
    IF @PageSize > 200 SET @PageSize = 200;

    SELECT
        st.Id,
        st.SenderEmail,
        st.Subject,
        st.MailContent,
        st.ReceivedAt,
        st.Intent,
        st.PrimarySpecialization,
        st.MailBodyIntent,
        st.AttachmentJobDescription,
        st.AttachmentPdf,
        st.AttachmentResumes,
        st.DriveFolderLink,
        st.SummaryMail,
        st.SummaryCandidates,
        st.SummaryInterviewScores,
        st.UserId,
        st.ClientId,
        st.Status,
        st.CreatedAt,
        c.[Name]        AS ClientName,
        COUNT(*) OVER() AS TotalCount
    FROM  dbo.SupportTickets st
    LEFT  JOIN dbo.Client c ON c.id = st.ClientId
    WHERE
        (@Status      IS NULL OR st.Status      =  @Status)
        AND (@Intent      IS NULL OR st.Intent      =  @Intent)
        AND (@SenderEmail IS NULL OR st.SenderEmail LIKE '%' + @SenderEmail + '%')
        AND (@DateFrom    IS NULL OR st.ReceivedAt  >= @DateFrom)
        AND (@DateTo      IS NULL OR st.ReceivedAt   < DATEADD(DAY, 1, @DateTo))
    ORDER BY st.ReceivedAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH  NEXT @PageSize ROWS ONLY;
END
GO
