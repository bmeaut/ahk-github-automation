using GradeManagement.Client.Network;


namespace GradeManagement.Client.Services;

public class SubjectService
{
    private readonly SubjectClient client;
    private SelectedSubjectService _currentSubject;

    public SubjectService(SelectedSubjectService selectedSubjectService, SubjectClient client)
    {
        this.client = client;
        _currentSubject = selectedSubjectService;
    }

    public SubjectResponse CurrentSubject
    {
        get => _currentSubject.CurrentSubject;
        set
        {
            if (_currentSubject.CurrentSubject == value) return;
            _currentSubject.CurrentSubject = value;
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
        if (_currentSubject.CurrentSubject is null && Subjects.Count > 0)
        {
            _currentSubject.CurrentSubject = Subjects[0];
        }

        return Subjects;
    }
}
