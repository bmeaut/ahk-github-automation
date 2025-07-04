using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.Dal.Entities;

namespace Ahk.GradeManagement.Bll.Services;

public class AssignmentLogService(GradeManagementDbContext gradeManagementDbContext)
{
    public async Task<AssignmentLog> CreateAsync(AssignmentLog assignmentLog)
    {
        gradeManagementDbContext.Add(assignmentLog);
        await gradeManagementDbContext.SaveChangesAsync();
        return assignmentLog;
    }
}
