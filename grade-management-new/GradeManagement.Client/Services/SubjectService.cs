using GradeManagement.Client.Network;


namespace GradeManagement.Client.Services;

public class SubjectService(SubjectClient client)
{
    private Subject _currentSubject;

    public Subject CurrentSubject
    {
        get => _currentSubject;
        set
        {
            if (_currentSubject == value) return;
            _currentSubject = value;
            NotifyStateChanged();
        }
    }

    public List<Subject> Subjects { get; private set; }

    public event Action OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();

    public async Task<List<Subject>> LoadSubjects()
    {
        Subjects = (await client.GetAllAsync()).ToList();
        if (_currentSubject is null && Subjects.Count > 0)
        {
            _currentSubject = Subjects[0];
        }

        return Subjects;
    }
}
