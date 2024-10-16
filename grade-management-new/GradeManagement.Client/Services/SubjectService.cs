using GradeManagement.Client.Network;


namespace GradeManagement.Client.Services;

public class SubjectService
{
    private readonly SubjectClient client;
    private SubjectResponse _currentSubject;

    public SubjectService(IServiceProvider serviceProvider)
    {
        client = serviceProvider.GetRequiredService<SubjectClient>();
    }

    public SubjectResponse CurrentSubject
    {
        get => _currentSubject;
        set
        {
            if (_currentSubject == value) return;
            _currentSubject = value;
            NotifyStateChanged();
        }
    }

    public List<SubjectResponse> Subjects { get; private set; }

    public event Action OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();

    public async Task<List<SubjectResponse>> LoadSubjects()
    {
        Subjects = (await client.GetAllAsync()).ToList();
        //Subjects = [];
        if (_currentSubject is null && Subjects.Count > 0)
        {
            _currentSubject = Subjects[0];
        }

        return Subjects;
    }
}
