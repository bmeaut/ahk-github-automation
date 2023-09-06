
namespace DTOs
{
    public class GroupDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Room { get; set; }
        public string Time { get; set; }
        public List<StudentDTO> Students { get; set; }

    }
}
