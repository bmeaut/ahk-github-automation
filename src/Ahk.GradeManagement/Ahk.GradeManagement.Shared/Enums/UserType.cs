namespace GradeManagement.Shared.Enums;

public enum UserType
{
    User = 1, //Has no special permissions
    Teacher = 2, //Can create subjects
    Admin = 3 //Has full control over the system
}
