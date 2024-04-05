using AutoMapper;

using AutSoft.Linq.Queryable;

using GradeManagement.Data.Data;
using GradeManagement.Data.Models;

using Microsoft.EntityFrameworkCore;

using GroupTeacher = GradeManagement.Data.Models.GroupTeacher;

namespace GradeManagement.Bll;

public class GroupTeacherService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
{
    public async Task<List<GroupTeacher>> UpdateTeachersByGroupIdAsync(long groupId, List<User> teachers)
    {
        var group = await gradeManagementDbContext.Group.SingleEntityAsync(g => g.Id == groupId, groupId);

        var groupTeachers = await gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == groupId)
            .ToListAsync();

        var groupTeachersToDelete = groupTeachers
            .Where(gt => !teachers.Select(t => t.Id).Contains(gt.UserId))
            .ToList();
        var groupTeachersToAdd = teachers
            .Where(t => !groupTeachers.Select(gt => gt.UserId).Contains(t.Id))
            .Select(t => new GroupTeacher
            {
                GroupId = groupId,
                UserId = t.Id
            })
            .ToList();

        gradeManagementDbContext.GroupTeacher.RemoveRange(groupTeachersToDelete);
        await gradeManagementDbContext.GroupTeacher.AddRangeAsync(groupTeachersToAdd);
        await gradeManagementDbContext.SaveChangesAsync();

        return await gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == groupId)
            .ToListAsync();
    }
}
