﻿using GradeManagement.Data;
using GradeManagement.Shared.Enums;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class SubjectTeacherService(GradeManagementDbContext gradeManagementDbContext)
{
    public async Task<Dictionary<long, UserRoleOnSubject>> GetAllSubjectsWithRolesForTeacherAsync(long teacherId)
    {
        var subjectForTeacher = await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.UserId == teacherId)
            .ToListAsync();

        return subjectForTeacher.ToDictionary(subject => subject.SubjectId, subject => subject.Role);
    }
}
