using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.SetGrade
{
    public class SetGradeService : ISetGradeService
    {
        private readonly AhkDb db;

        public SetGradeService(AhkDb db)
            => this.db = db;

        public async Task SetGrade(SetGradeEvent data)
        {
            await this.db.EnsureCreated();
            this.db.Results.Add(StudentResult.Create(data.Neptun, data.Repository, data.PrNumber, data.PrUrl, data.Actor, data.Origin, getPoints(data.Results)));
            await this.db.SaveChangesAsync();
        }

        private static List<ExerciseWithPoint> getPoints(double[] values)
        {
            var value = new List<ExerciseWithPoint>(capacity: values.Length);
            for (int i = 0; i < values.Length; i++)
                value.Add(new ExerciseWithPoint() { Name = $"ex{i}", Point = values[i] });
            return value;
        }
    }
}
