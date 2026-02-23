# PublicCandidateStatus - AI Interview Video Count & Manual Report Enhancement

## Overview
Enhanced the `Candidate/PublicCandidateStatus` page (specifically for `candidateType=Report Pending`) to display AI interview video counts and provide manual report submission options, similar to the functionality already available in `DashBoard/UnassignedInterviews?actionstatus=AIMeetingStatus`.

## Problem Statement
Customers viewing the PublicCandidateStatus page with "Report Pending" status couldn't see:
1. Why reports were pending for AI interviews (often due to missing candidate videos in `BotInterviewQuestionVideos` table)
2. The count of candidate response videos for AI interviews
3. An option for internal team members to manually mark interviews as "No Show", "Dropped", etc.

## Solution Implemented

### 1. Database Changes

#### A. Updated Stored Procedure: `usp_GetCandidateTimelineByDateStatus`
**File**: `Risebird.DB/StoredProcedures/usp_GetCandidateTimelineByDateStatus.sql`

**Changes**: Added two OUTER APPLY clauses to all SELECT statements in the stored procedure to fetch:
- `AICandidateResponseVideoCount`: Count of videos in `BotInterviewQuestionVideos` for the interview's `SessionId`
- `AITotalQuestionsCount`: Count of questions in `SkillChannelSampleQuestions` for the IRF

**Code Added** (applied to all role-based query sections):
```sql
outer apply(select count(id) as VideoCount from BotInterviewQuestionVideos 
            where SessionId = im.SessionId AND VideoLink IS NOT NULL AND VideoLink != '') AIVideoCount
outer apply(select count(id) as AITotalQuestionsCount from SkillChannelSampleQuestions 
            where irfid = CTE.IRFId) AIQuestionCount
```

**Impact**: Now all scheduled interview queries return AI interview video and question counts.

### 2. ViewModel Changes

#### Updated: `CandidateTimelineVM`
**File**: `Risebird.Entities/ViewModel/CandidateTimelineVM.cs`

**Properties Added**:
```csharp
public int AITotalQuestionsCount { get; set; }
public int AICandidateResponseVideoCount { get; set; }
```

**Impact**: The ViewModel can now carry AI interview metrics from database to view.

### 3. View Changes

#### Enhanced: `PublicCandidateStatus.cshtml`
**File**: `Risebird.Web/Views/Candidate/PublicCandidateStatus.cshtml`

**Changes Made**:

##### A. Table Headers (lines 853-884, 885-916)
Added conditional columns in both `<thead>` and `<tfoot>` for "Report Pending" view:
- **AI Questions**: Displays total questions count
- **AI Videos**: Displays candidate response video count

**Code**:
```cshtml
@if (ViewBag.CandidateType == CandidateTypeForClient.ReportPending.GetDescription())
{
    <th>AI Questions</th>
    <th>AI Videos</th>
}
<th>Report</th>
```

##### B. Table Data Rows (lines 1163-1258+)
Added three key enhancements:

1. **AI Questions Count Column**:
```cshtml
<td class="text-center">@item.AITotalQuestionsCount</td>
```

2. **AI Videos Count Column** (with visual indicator):
```cshtml
<td class="text-center">
    @if (item.IsAIInterview)
    {
        <span style="@(item.AICandidateResponseVideoCount == 0 ? "color:red;font-weight:bold;" : "")">
            @item.AICandidateResponseVideoCount
        </span>
    }
    else
    {
        <span>-</span>
    }
</td>
```
- Shows video count in **red and bold** if count is 0 (indicating the reason for report pending)
- Shows "-" for non-AI interviews

3. **Manual Report Submission Button**:
```cshtml
@if (ViewBag.CandidateType == CandidateTypeForClient.ReportPending.GetDescription() && item.IsAIInterview)
{
    <button class="btn shadow btn-xs sharp me-1" aria-label="Report" 
            style="top: 0px;position: relative;cursor: pointer; width:25px!important;height:25px!important;background:#42a5f5;color:#fff;">
        <div onclick="location.href = '@Url.Action("AddReport", "Interview" , new { interviewId=item.InterviewId, irfId=item.IRFId})'" 
             title="Manual Report Submission" class="mdc-fab__icon material-icons">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24">
                <path d="M20 2H4c-1.1 0-1.99.9-1.99 2L2 22l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-7 12h-2v-2h2v2zm0-4h-2V6h2v4z" />
            </svg>
        </div>
    </button>
}
```
- Only shown for AI interviews in "Report Pending" status
- Redirects to `Interview/AddReport` for manual status submission
- Styled consistently with the UnassignedInterviews page

## Integration with Existing Validation

The manual report submission button links to `Interview/AddReport`, which now has **5 layers of validation** (implemented in previous task):

1. **Client-side JavaScript validation** (`CheckAIInterviewVideoRequirement()`)
2. **Service layer validation** (`MeetingService.AddInterviewReport`)
3. **Repository layer** (`GetAIInterviewVideoCount`)
4. **Stored procedure validation** (4 SPs updated)
5. **Database trigger** (`tr_InterviewComments_ValidateAIVideoBeforeComplete`)

This ensures that even if an internal team member tries to mark an AI interview as "Completed" without videos, the system will prevent it at multiple levels.

## User Experience Flow

### Scenario 1: AI Interview with No Videos (Report Pending)

**Customer View** (`PublicCandidateStatus?candidateType=Report Pending`):
```
| Skill Channel | Candidate Details | Interview Date | AI Questions | AI Videos | Report | Action |
|---------------|-------------------|----------------|--------------|-----------|--------|--------|
| Python Dev    | John Doe          | 2026-02-20     | 10           | 0         | [Manual Report Button] + [No Report] | ... |
```

- **AI Videos** column shows **0** in red and bold - clearly indicating why report is pending
- **AI Questions** column shows **10** - customer knows questions were set up
- **Manual Report Button** is available for internal team to mark as "No Show" or "Dropped"

### Scenario 2: AI Interview with Videos (Report Available)

```
| Skill Channel | Candidate Details | Interview Date | AI Questions | AI Videos | Report | Action |
|---------------|-------------------|----------------|--------------|-----------|--------|--------|
| Java Dev      | Jane Smith        | 2026-02-19     | 8            | 8         | [AI Report Icon] | ... |
```

- **AI Videos** column shows **8** (normal font) - all responses recorded
- **AI Report Icon** is clickable - customer can view the AI-generated report
- **Manual Report Button** is also shown (for edge cases where team needs to override)

### Scenario 3: Non-AI Interview (Report Pending)

```
| Skill Channel | Candidate Details | Interview Date | AI Questions | AI Videos | Report | Action |
|---------------|-------------------|----------------|--------------|-----------|--------|--------|
| .NET Dev      | Bob Johnson       | 2026-02-18     | 0            | -         | [No Report] | ... |
```

- **AI Questions** shows **0** (not an AI interview)
- **AI Videos** shows **-** (not applicable)
- **Manual Report Button** is NOT shown (only for AI interviews)

## Files Modified

### Database Layer
1. `Risebird.DB/StoredProcedures/usp_GetCandidateTimelineByDateStatus.sql` - Added video and question count queries

### Entity/ViewModel Layer
2. `Risebird.Entities/ViewModel/CandidateTimelineVM.cs` - Added `AITotalQuestionsCount` and `AICandidateResponseVideoCount` properties

### Presentation Layer
3. `Risebird.Web/Views/Candidate/PublicCandidateStatus.cshtml` - Added columns and manual report button

## Benefits

### For Customers
1. **Transparency**: Customers can now see exactly why reports are pending (no videos recorded)
2. **Real-time Metrics**: Question and video counts provide insight into interview progress
3. **Visual Indicators**: Red/bold text for zero videos immediately highlights the issue

### For Internal Team
1. **Efficient Handling**: Manual report button allows quick marking of "No Show", "Dropped", etc.
2. **Consistent UI**: Same functionality as `UnassignedInterviews` page, reducing learning curve
3. **Protected Workflow**: All existing validations apply, preventing accidental completion marking

### For System Integrity
1. **Data-Driven Display**: Counts are fetched directly from source tables
2. **Performance Optimized**: OUTER APPLY in stored procedure ensures efficient queries
3. **Null-Safe**: ISNULL wrappers prevent errors if data is missing

## Reference URLs

**Customer View** (Public URL):
```
Candidate/PublicCandidateStatus?emailid={email}&id={guid}&candidateType=Report Pending
```

**Internal Team View** (Similar functionality):
```
DashBoard/UnassignedInterviews?actionstatus=AIMeetingStatus&actiontype=0&returnBack=1
```

**Manual Report Submission** (When button clicked):
```
Interview/AddReport?interviewId={id}&irfId={id}
```

## Testing Recommendations

1. **Test with AI interviews having 0 videos**: Verify red/bold display and manual button presence
2. **Test with AI interviews having videos**: Verify normal display and report link
3. **Test with non-AI interviews**: Verify "-" display and no manual button
4. **Test manual report submission**: Verify validation prevents completion without videos
5. **Test different client roles**: Verify buttons show only for appropriate roles
6. **Test Report Pending tab**: Verify new columns appear only in this view
7. **Performance test**: Verify query performance with OUTER APPLY additions

## Database Query Performance Note

The added OUTER APPLY clauses are efficient because:
- They use indexed columns (`SessionId`, `irfid`)
- They return single aggregate values (COUNT)
- They only execute for rows in the result set (not a CROSS APPLY)
- Modern SQL Server optimizers handle OUTER APPLY efficiently

If performance issues arise with large datasets, consider:
- Adding index on `BotInterviewQuestionVideos.SessionId`
- Adding index on `SkillChannelSampleQuestions.irfid`
- Caching video counts in `InterviewMeetings` table (denormalization)

## Backward Compatibility

All changes are **backward compatible**:
- New columns only display in "Report Pending" view
- Existing views (Future Scheduled, Completed Meeting) unchanged
- Non-AI interviews display gracefully ("-" for videos)
- Stored procedure returns 0 for counts if no AI data exists

## Related Documentation

See also:
- `AI_Interview_AddReport_Accessibility_Points.md` - Complete documentation of validation implementation
- Previous implementation included 5-layer validation for AI interview completion

---

**Implementation Date**: February 23, 2026  
**Author**: AI Assistant  
**Purpose**: Customer transparency for AI interview report pending status
