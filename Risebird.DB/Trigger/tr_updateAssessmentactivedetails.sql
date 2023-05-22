IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_updateAssessmentactivedetails')
BEGIN
    Drop trigger tr_updateAssessmentactivedetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER tr_updateAssessmentactivedetails 
   ON  InterviewerProfile
   AFTER Update
AS 
BEGIN
Declare @AssessmentGroupIds varchar(max)
Declare @Userid int
DECLARE @tbl_assessmentid TABLE (            
    id int
)
Select @AssessmentGroupIds=i.AssessmentGroupId,@Userid=i.UserId from inserted i
Insert into @tbl_assessmentid Select value as id from string_split(@AssessmentGroupIds,',')

update ti set ti.isactive=0 from TransInterviewer ti join IRF on ti.IRFId=IRF.id 
left join @tbl_assessmentid tb on tb.id=IRF.AssessmentGroupid
where ti.UserId=@Userid and ti.IsActive=1 and tb.id is null and IRF.AssessmentGroupid is not null

update ti set ti.isactive=0 from TransClonableIRFInterviewer ti 
join ClonableSkillChannel csc on csc.id = ti.ClonableIRFId
join IRF on csc.CreatedFromIRFId=IRF.id 
left join @tbl_assessmentid tb on tb.id=IRF.AssessmentGroupid
where ti.UserId=@Userid and ti.IsActive=1 and tb.id is null and IRF.AssessmentGroupid is not null

END
GO


