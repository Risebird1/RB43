# AI Interview AddReport Form - Accessibility Points Documentation

## Overview
This document lists all the places from where the AddReport form is accessible for AI interviews in the Risebird application. The AddReport form is used to manually submit interview status (complete, selected, rejected, etc.).

## Controllers with AddReport Actions

### 1. Interview Controller
**Path**: `Risebird.Web/Controllers/InterviewController.cs`

**Action Methods**:
- `AddReport(int interviewId, int irfId, bool isClient = false, string actiontype = null)`
  - Primary entry point for interview report submission
  - Handles both AI and non-AI interviews
  - Now includes validation to check for AI interview videos before marking as complete

**Validation Implemented**:
- Service layer validation in `MeetingService.AddInterviewReport`
- Service layer validation in `MeetingService.SaveAIInterviewProctorEvaluation`
- Client-side validation in `Views/Interview/AddReport.cshtml`
- Controller-level exception handling for `InvalidOperationException`

### 2. Interviewer Controller
**Path**: `Risebird.Web/Controllers/InterviewerController.cs`

**Action Methods**:
- `AddReport(int interviewId, int iscId, string backUrl = null)`
  - Used for interviewer assessment reports
  - May handle AI interviews if interviewer is assessing them

### 3. IRF Controller
**Path**: `Risebird.Web/Controllers/IRFController.cs`

**Action Methods**:
- `AddReport(int interviewId, int irfId, bool IsSelected = false)`
  - Used for IRF-specific interview reports
  - Includes validation for AI interviews

### 4. InstaInterview Controller
**Path**: `Risebird.Web/Controllers/InstaInterviewController.cs`

**Action Methods**:
- `AddReport(int interviewId, int Requestedid)`
  - Used for instant interview reports
  - May handle AI-based instant interviews

## UI Access Points

### Dashboard Views

#### 1. UnassignedInterviews (Primary Entry Point for AI Interviews)
**Path**: `Risebird.Web/Views/Dashboard/UnassignedInterviews.cshtml`

**URL**: `/Dashboard/UnassignedInterviews?actionstatus=AIMeetingStatus&actiontype=0&returnBack=1`

**Description**: 
- Lists all AI interviews (where `IsAIInterview = 1` in IRF table)
- Shows interviews pending status update
- Multiple buttons/links to AddReport:
  - Line 856: Standard report button
  - Line 1045: Report button for AI-specific interviews
  - Line 1176: Report link for interview meetings

**Conditional Logic**:
- Line 1047: Shows button only if `AICandidateResponseVideoCount > 0`
- Already has partial UI awareness of video requirement

#### 2. SensitiveData
**Path**: `Risebird.Web/Views/Dashboard/SensitiveData.cshtml`

**Description**:
- Line 407: Provides link to AddReport for interviews requiring special handling

### Interview Views

#### 3. Index (Interview Listing)
**Path**: `Risebird.Web/Views/Interview/Index.cshtml`

**Description**:
- Line 941: Report button for each interview in the list
- Line 1005: Additional report button for filtered views

#### 4. MyInterview
**Path**: `Risebird.Web/Views/Interview/MyInterview.cshtml`

**Description**:
- Line 548: Report button in "My Interviews" view
- Shows interviews assigned to current user

#### 5. MyInterviewPartial
**Path**: `Risebird.Web/Views/Interview/MyInterviewPartial.cshtml`

**Description**:
- Line 103: Report link in partial view
- Used when rendering interview lists in modals/AJAX calls

#### 6. MyInterviewerAssessments
**Path**: `Risebird.Web/Views/Interview/MyInterviewerAssessments.cshtml`

**Description**:
- Line 330: Report link for interviewer assessments

#### 7. CompletedInterviews
**Path**: `Risebird.Web/Views/Interview/CompletedInterviews.cshtml`

**Description**:
- Line 347: Report button for completed interviews
- Allows editing/viewing reports post-completion

### Candidate Views

#### 8. CandidateTimeline
**Path**: `Risebird.Web/Views/Candidate/CandidateTimeline.cshtml`

**Description**:
- Line 750: Report button in candidate timeline view
- Line 1430: Additional report button for different timeline sections

#### 9. PublicCandidateStatus
**Path**: `Risebird.Web/Views/Candidate/PublicCandidateStatus.cshtml`

**Description**:
- Line 2394: Public report link (JavaScript generated)
- Allows candidates/public users to view certain reports

### Interviewer Skill/Assessment Views

#### 10. GetISCInterviewers
**Path**: `Risebird.Web/Views/InterviewerSkills/GetISCInterviewers.cshtml`

**Description**:
- Line 130: Report button for interviewer skill channel interviews

#### 11. UnapprovedInterviewerMeetings
**Path**: `Risebird.Web/Views/UnapprovedInterviewerMeetings/Index.cshtml`

**Description**:
- Line 346: Report button for unapproved interviewer meetings

#### 12. MyInterviewerAssessment (Interviewer Module)
**Path**: `Risebird.Web/Views/Interviewer/MyInterviewerAssessment.cshtml`

**Description**:
- Line 182: Report button for interviewer's own assessments
- Line 649: JavaScript-generated report link

### InstaInterview Views

#### 13. InstaInterview Index
**Path**: `Risebird.Web/Views/InstaInterview/Index.cshtml`

**Description**:
- Line 338: Report link for instant interviews
- Line 344: Report button for instant interview listings

### Project/IRF Views

#### 14. SelectedInterviewMeetings (Project)
**Path**: `Risebird.Web/Views/Project/SelectedInterviewMeetings.cshtml`

**Description**:
- Line 124: Report link for selected interview meetings in projects

#### 15. ProjectInterviewMeetings
**Path**: `Risebird.Web/Views/Project/ProjectInterviewMeetings.cshtml`

**Description**:
- Contains links to AddReport for project-related interviews

#### 16. InterviewMeetings (IRF)
**Path**: `Risebird.Web/Views/IRF/InterviewMeetings.cshtml`

**Description**:
- Report links for IRF interview meetings

#### 17. SelectedInterviewMeetings (IRF)
**Path**: `Risebird.Web/Views/IRF/SelectedInterviewMeetings.cshtml`

**Description**:
- Report links for selected meetings in IRF context

### Other Access Points

#### 18. SkillChannelSpeedDetails (Task Module)
**Path**: `Risebird.Web/Views/Task/SkillChannelSpeedDetails.cshtml`

**Description**:
- Line 57: Report button for skill channel interview details

#### 19. QCReportForm Redirects
**Path**: `Risebird.Web/Controllers/InterviewController.cs`

**Description**:
- Lines 6368, 6419, 6907: Redirects back to AddReport after QC report submission

## Validation Layers Implemented

### 1. Client-Side Validation
**Location**: `Risebird.Web/Views/Interview/AddReport.cshtml`

**Function**: `CheckAIInterviewVideoRequirement()`
- Checks if interview is AI interview
- Validates that video count > 0 before allowing MeetingStatus = 4 (Completed)
- Shows user-friendly alert message

### 2. Service Layer Validation
**Location**: `Risebird.Business/MeetingService.cs`

**Methods**:
- `AddInterviewReport`: Checks video count before saving report
- `SaveAIInterviewProctorEvaluation`: Checks video count before saving evaluation
- Throws `InvalidOperationException` with clear message if validation fails

### 3. Repository Layer
**Location**: `Risebird.Repository/MeetingRespository.cs`

**Method**: `GetAIInterviewVideoCount(string sessionId)`
- Queries `BotInterviewQuestionVideos` table
- Returns count of videos with non-null, non-empty `VideoLink`

### 4. Database Stored Procedures
**Locations**: Multiple stored procedures updated

**Procedures**:
1. `usp_UpdateMeetingCompletedStatusForAIInterview`
2. `usp_UpdateInterviewReportComments`
3. `usp_UpdateInterviewComments`
4. `usp_SaveAIInterviewProctorEvaluation`

**Validation Logic**:
- Checks `IsAIInterview` flag from IRF table
- Queries `BotInterviewQuestionVideos` for video count
- Raises `RAISERROR` if attempting to set MeetingStatus = 4 with no videos
- Rolls back transaction where applicable

### 5. Database Trigger (Final Safety Net)
**Location**: `Risebird.DB/Trigger/tr_InterviewComments_ValidateAIVideoBeforeComplete.sql`

**Trigger**: `tr_InterviewComments_ValidateAIVideoBeforeComplete`
- Fires on `INSERT` or `UPDATE` of `InterviewComments` table
- Validates AI interview + video requirement before allowing MeetingStatus = 4
- Rolls back transaction if validation fails
- Provides detailed error message

## Audit Trail

### Existing Audit Mechanisms
**Location**: `Risebird.DB/Trigger/`

**Triggers**:
1. `tr_InterviewCommentsHistory_ForInsert`: Logs all inserts to `InterviewComments`
2. `tr_InterviewCommentsHistory_ForUpdate`: Logs all updates to `InterviewComments`

**Captured Information**:
- `CreatedBy` / `ModifiedBy`: User who made the change
- `CreatedOn` / `ModifiedOn`: Timestamp of change
- `PerformedBy`: Mapped from CreatedBy/ModifiedBy
- `PerformedAt`: Timestamp adjusted for IST (UTC + 330 minutes)
- `ActionType`: 'Inserted' or 'Updated'
- All column values from `InterviewComments` table

**History Table**: `InterviewCommentsHistory`

## Summary

### Total Access Points: 19+ locations across the application

### Primary Entry Point for AI Interviews:
`/Dashboard/UnassignedInterviews?actionstatus=AIMeetingStatus`

### Protection Layers: 5 levels
1. Client-side JavaScript validation
2. C# Service layer validation
3. C# Repository layer data fetch
4. SQL Stored procedure validation (4 procedures)
5. SQL Database trigger (final safety net)

### Audit Trail: Fully implemented
- All changes to `InterviewComments` are logged with user and timestamp
- Historical data preserved in `InterviewCommentsHistory` table

## Implementation Status

✅ **Completed**:
- All validation layers implemented
- All stored procedures updated
- Database trigger created
- Client-side validation added
- Service layer validation added
- Repository methods implemented
- Exception handling in controllers
- Audit trail confirmed working

## Testing Recommendations

1. **Test Primary Flow**: 
   - Navigate to `/Dashboard/UnassignedInterviews?actionstatus=AIMeetingStatus`
   - Attempt to mark AI interview as complete without videos
   - Verify validation blocks the action

2. **Test Each Access Point**:
   - Verify validation works from all 19+ access points
   - Test both AI and non-AI interviews

3. **Test Edge Cases**:
   - Interview with videos deleted after completion
   - Partial video data
   - Invalid SessionId

4. **Verify Audit Trail**:
   - Check `InterviewCommentsHistory` after each status change
   - Verify `PerformedBy` and `PerformedAt` are correctly logged

## Notes for Developers

1. The `AIVideoCount` property in `InterviewReport` model is populated in `GetInteviewReport` method
2. All AI interview validations check for `SessionId` presence
3. Video validation requires: `VideoLink IS NOT NULL AND VideoLink != ''`
4. The validation is consistent across all layers for maintainability
5. Error messages are user-friendly and descriptive

---

**Document Version**: 1.0  
**Last Updated**: February 23, 2026  
**Author**: AI Assistant  
**Purpose**: Documentation of AI Interview completion validation implementation
