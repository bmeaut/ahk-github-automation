using AutoMapper;

using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class DashboardService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public DashboardService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Dashboard>> GetAllDashboardDtosForSubjectAsync(long subjectId)
    {
        var assignments = _gradeManagementDbContext.Assignment
            .Include(a => a.Student)
            .Include(a => a.Exercise).ThenInclude(e => e.Course)
            .Include(a => a.PullRequests).ThenInclude(pr => pr.Scores).ThenInclude(s => s.ScoreType)
            .Where(a => a.Exercise.Course.SubjectId == subjectId);
        var dashboardDtos = new List<Dashboard>();
        foreach (var assignment in assignments)
        {
            var dashboardDto = new Dashboard
            {
                AssignmentName = assignment.GithubRepoName,
                GithubRepoUrl = assignment.GithubRepoUrl,
                ExerciseName = assignment.Exercise.Name,
                CourseName = assignment.Exercise.Course.Name,
                PullRequests = _mapper.Map<List<PullRequestForDashboard>>(assignment.PullRequests),
                StudentNeptun = assignment.Student.NeptunCode
            };
            dashboardDtos.Add(dashboardDto);
        }
        return dashboardDtos;
    }
}
