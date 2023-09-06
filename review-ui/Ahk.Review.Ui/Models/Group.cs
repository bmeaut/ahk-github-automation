using DTOs;

namespace Ahk.Review.Ui.Models
{
    public class Group
    {
        public Group(GroupDTO groupDTO)
        {
            this.Id = groupDTO.Id;
            this.Name = groupDTO.Name;
            this.Room = groupDTO.Room;
            this.Time = groupDTO.Time;
            this.Students = groupDTO.Students.Select(sDTO =>
            {
                return new Student(sDTO);
            }).ToList();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Room { get; set; }
        public string Time { get; set; }
        public List<Student> Students { get; set; }
    }
}
