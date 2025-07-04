using GradeManagement.Data;
using GradeManagement.Data.Models;

namespace GradeManagement.Bll.Services;

public class AssignmentLogService(GradeManagementDbContext gradeManagementDbContext)
{
    public async Task<AssignmentLog> CreateAsync(AssignmentLog assignmentLog)
    {
        gradeManagementDbContext.Add(assignmentLog);
        await gradeManagementDbContext.SaveChangesAsync();
        return assignmentLog;
    }
}
