﻿using GradeManagement.Data.Models.Interfaces;

namespace GradeManagement.Data.Models;

public class Score : ISoftDelete, ITenant
{
    public long Id { get; set; }
    public double Value { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public ScoreType ScoreType { get; set; }
    public long ScoreTypeId { get; set; }
    public PullRequest PullRequest { get; set; }
    public long PullRequestId { get; set; }
    public User? Teacher { get; set; }
    public long? TeacherId { get; set; }
    public List<ScoreTypeExercise> ScoreTypeExercises { get; set; }
    public bool IsDeleted { get; set; }
    public long SubjectId { get; set; }
}
