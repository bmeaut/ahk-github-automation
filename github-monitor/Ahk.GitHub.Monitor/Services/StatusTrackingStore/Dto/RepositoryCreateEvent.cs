using System;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

// AssignmentAcceptedEvent
public class RepositoryCreateEvent(string gitHubRepositoryUrl) : StatusEventBase(gitHubRepositoryUrl);
