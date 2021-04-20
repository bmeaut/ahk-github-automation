# Ahk GitHub monitor

A GitHub App hosted as an Azure Function processing GitHub webhooks. The purpose is to monitor GitHub repositories of student homework assignments for conformance with the expected workflow.

The basis of operation is

1. a GitHub organization where the student assignments are stored, managed by GitHub Classroom;
1. a running instance of this application hosted as an Azure Function with a public URL;
1. and a GitHub App pointing to the Azure function, added to the GitHub organization (handling events in _all_ repository of the GitHub organization).

## ~~Public~~ GitHub App

The GitHub App built from this source is **private**, but feel free to fork the source code and create your own.

## Global configuration of the GitHub App

The application requires the following configurations specified as **environment variables**.

`AHK_GitHubAppId` and `AHK_GitHubAppPrivateKey`: (**mandatory**) the ID and the private key of the GitHub App (both available in GitHub App management page).

> All GitHub interactions from the application will be performed in the name of this GitHub App.

`AHK_GitHubWebhookSecret`: (**mandatory**) the secret that is configured in the GitHub App. The secret is mandatory; unsecured webhook calls are rejected by the application.

## Enable for a repository

The GitHub app is installed in a GitHub organization and hence receives events for all kinds of activities within that organization. To limit the scope of this application, each repository that this GitHub App should monitor must have a `.github/ahk-monitor.yml` file in the default branch with the following content

```yaml
enabled: true
```

If this file is not present, it's content is not `enabled` / `enabled: true` / `enabled: yes`, all fired events are ignored for the specific repository.

## Rules enforced by the application

The application enforces the following rules.

### Branch usage

> The default branch is not touched by the student. The solution is submitted on a new branch via a pull request.
> 
> There are no force pushes.

These rules are enforced by marking all branches as protected, and requiring review of pull requests targeting the default branch.

### Issue comment editing

> No user edits or deletes an issue comment made by someone else.

This rule is ensured by registering such an attempt and adding a comment into the issue thread where the edit/delete took place.

### Single pull request for submitting work

> There is no more than one open pull request at any time.
> 
> A pull request should not be opened once a PR has been closed by the teacher.

Explanation: An open pull requests is a submission of the work. There can only be one submission. A submission can be revoked by closing the pull requests. If a submission has already been evaluated (the PR has been closed by the teacher), no more submission (new PRs) are allowed.

These rules are ensured by registering such an event and adding a comment into the affected PR issue threads.

### Reviewer must be assigned to the pull request

> Pull requests should be assigned to the instructor for evaluation. When a review is requested instead, assign the instructor.

Explanation: To track submitted work the pull requests should be assigned to the instructor. This allows the instructor to use GitHub web interface to go through all submissions. Requesting a review of a pull request is frequently mixed up with assignment. Although GitHub has a list of requested reviews, if the instructor choses to request a change in the submission, the pull request is removed from this list. If the student makes changes to the pull request as the result of the change request, the pull request does not show up in the pending reviews list. To ensure tracking these pull requests too, any reviewer is automatically set as assignee too.
