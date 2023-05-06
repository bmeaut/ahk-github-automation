using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data.Internal
{
    public class GradeRepository : IGradeRepository
    {
        public AhkDbContext Context { get; set; }
        public GradeRepository()
        {
        }

        public async Task AddGrade(Grade value)
        {
            Context.Grades.Add(value);
            await Context.SaveChangesAsync();
        }
        public async Task<Grade> GetLastResultOf(string neptun, string gitHubRepoName, int gitHubPrNumber)
        {
            var grades = Context.Grades
                .Where(s => s.Student.Neptun == neptun.ToUpperInvariant() && s.GithubRepoName == gitHubRepoName.ToLowerInvariant() && s.GithubPrNumber == gitHubPrNumber)
                .OrderByDescending(s => s.Date);

            return grades.FirstOrDefault();
        }
        public async Task<IReadOnlyCollection<Grade>> ListConfirmedWithRepositoryPrefix(string repoPrefix)
        {
            var confirmedGrades = Context.Grades
                .Where(s => s.Confirmed && s.GithubRepoName.StartsWith(repoPrefix));
            return confirmedGrades.ToList().AsReadOnly();
        }

        public async Task DeleteGrade(int id)
        {
            var grade = Context.Grades.Find(id);

            Context.Grades.Remove(grade);
            await Context.SaveChangesAsync();
        }

    }
}
