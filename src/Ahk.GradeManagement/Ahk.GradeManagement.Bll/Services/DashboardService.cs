using AutoMapper;

using GradeManagement.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class DashboardService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
{
    public async Task<IEnumerable<Dashboard>> GetAllDashboardDtosForSubjectAsync(long subjectId)
    {
        var assignments = await gradeManagementDbContext.Assignment
            .Include(a => a.Student)
            .Include(a => a.Exercise).ThenInclude(e => e.Course)
            .Include(a => a.PullRequests).ThenInclude(pr => pr.Scores).ThenInclude(s => s.ScoreType)
            .Where(a => a.Exercise.Course.SubjectId == subjectId).ToListAsync();
        var dashboardDtos = new List<Dashboard>();
        foreach (var assignment in assignments)
        {
            var dashboardDto = new Dashboard
            {
                AssignmentName = assignment.GithubRepoName,
                GithubRepoUrl = assignment.GithubRepoUrl,
                ExerciseName = assignment.Exercise.Name,
                CourseName = assignment.Exercise.Course.Name,
                PullRequests = mapper.Map<List<PullRequestForDashboard>>(assignment.PullRequests),
                StudentNeptun = assignment.Student.NeptunCode
            };
            dashboardDtos.Add(dashboardDto);
        }

        return dashboardDtos;
    }
}
