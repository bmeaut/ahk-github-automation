using AutSoft.Linq.Queryable;

using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Shared.Enums;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class SubjectTeacherService(GradeManagementDbContext gradeManagementDbContext)
{
    public async Task<UserRoleOnSubject?> GetRoleIfConnectionExistsAsync(long teacherId, long subjectId)
    {
        return await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == subjectId && st.UserId == teacherId)
            .Select(st => st.Role)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<long, UserRoleOnSubject>> GetAllSubjectsWithRolesForTeacherAsync(long teacherId)
    {
        var subjectForTeacher =  await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.UserId == teacherId)
            .ToListAsync();

        return subjectForTeacher.ToDictionary(subject => subject.SubjectId, subject => subject.Role);
    }

    public async Task<List<SubjectTeacher>> UpdateForSingleSubjectAsync(long subjectId, List<User> teachers)
    {
        var subjectTeachersForSubject = await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == subjectId)
            .ToListAsync();

        var teachersToAdd = teachers
            .Where(t => !subjectTeachersForSubject.Select(st => st.UserId).Contains(t.Id))
            .Select(t => new SubjectTeacher { SubjectId = subjectId, UserId = t.Id });

        var teachersToRemove = subjectTeachersForSubject
            .Where(st => !teachers.Select(t => t.Id).Contains(st.UserId));

        gradeManagementDbContext.SubjectTeacher.AddRange(teachersToAdd);
        gradeManagementDbContext.SubjectTeacher.RemoveRange(teachersToRemove);

        await gradeManagementDbContext.SaveChangesAsync();

        return await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == subjectId)
            .ToListAsync();
    }
}
