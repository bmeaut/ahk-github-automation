using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;

namespace Ahk.Review.Ui
{
    public class MapperConfig
    {
        public static Mapper InitializeAutomapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Assignment, AssignmentDTO>();
                cfg.CreateMap<Exercise, ExerciseDTO>();
                cfg.CreateMap<Grade, GradeDTO>();
                cfg.CreateMap<Group, GroupDTO>();
                cfg.CreateMap<Point, PointDTO>();
                cfg.CreateMap<User, UserDTO>();
            });

            var mapper = new Mapper(config);
            return mapper;
        }
    }
}
