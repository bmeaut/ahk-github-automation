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

    public event Action OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();

    public async Task<List<Subject>> LoadSubjects()
    {
        var subjects = (await client.GetALlAsync()).ToList();
        if (_currentSubject is null && subjects.Count > 0)
        {
            _currentSubject = subjects[0];
        }

        return subjects;
    }
}
