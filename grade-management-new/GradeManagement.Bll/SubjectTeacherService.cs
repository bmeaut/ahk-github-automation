using AutoMapper;

using GradeManagement.Data.Data;
using GradeManagement.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class SubjectTeacherService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;

    public SubjectTeacherService(GradeManagementDbContext gradeManagementDbContext)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
    }


    public async Task<List<SubjectTeacher>> UpdateForSingleSubjectAsync(long subjectId, List<User> teachers)
    {
        var subjectTeachersForSubject = await _gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == subjectId)
            .ToListAsync();

        var teachersToAdd = teachers
            .Where(t => !subjectTeachersForSubject.Select(st => st.TeacherId).Contains(t.Id))
            .Select(t => new SubjectTeacher { SubjectId = subjectId, TeacherId = t.Id });

        var teachersToRemove = subjectTeachersForSubject
            .Where(st => !teachers.Select(t => t.Id).Contains(st.TeacherId));

        _gradeManagementDbContext.SubjectTeacher.AddRange(teachersToAdd);
        _gradeManagementDbContext.SubjectTeacher.RemoveRange(teachersToRemove);

        await _gradeManagementDbContext.SaveChangesAsync();

        return await _gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == subjectId)
            .ToListAsync();
    }
}
