# Ahk GitHub monitor

This is an application hosted as an Azure Function processing GitHub webhooks. The purpose is to monitor GitHub repositories of student homework assignments for conformance with the expected workflow.

The basis of operation is

1. a GitHub organization where the student assignments are stored, managed by GitHub Classroom;
1. a running instance of this application hosted as an Azure Function with a public URL;
1. and an organization-wide webhook in GitHub pointing to the URL of this application (handling events in _all_ repository of the GitHub organization).

## Application configuration

The application requires the following configurations specified as **environment variables**.

`AHK_GITHUB_TOKEN`: (**mandatory**) a personal access token from GitHub that allows interaction with _all_ organization repositories.

> All GitHub interactions from the application will be performed in the name of this user.

`AHK_GITHUB_SECRET`: (**mandatory**) the secret that is configured in the GitHub webhook. The secret is mandatory; unsecured webhook calls are rejected by the application.

> Note, that it is possible to re-use the same application instance in multiple GitHub organization webhooks, but the secret used by these webhooks must be the same.

`AHK_GITHUB_REPOSITORY_PREFIXES`: (optional) a semicolon separated list of repository prefixes that are to be monitored. When a GitHub Classroom assignment is set up, a repository naming prefix is specified. Each student assignment repository name will start with the specified prefix. Since an organization may hold a wide variety of repositories, this configuration enables filtering for repositories of interest. An example value is `hw1-2020;labwork-aspnetcore-3`. If not specified, all repositories are monitored by the application.

## Rules enforced by the application

The application enforces the following rules.

### Branch usage

1. The master branch is not touched by the student. The solution is submitted on a new branch via a pull request.
1. There are not force pushes.

These rules are enforced by marking all branches as protected, and requiring review of pull requests targeting the master branch.

**Setup**: In order to enable this rule, the _Branch or tag creation_ GitHub webhook trigger must be enabled, and the `AHK_BRANCHPROTECTION_ENABLED` environment variable must be set to `1`.

### Issue comment editing

1. No user edits or deletes an issue comment made by someone else.

This rule is ensured by registering such an attempt and adding a comment into the issue thread where the edit/delete took place.

**Setup**: In order to enable this rule, the _Issue comments_ GitHub webhook trigger must be enabled and the `AHK_COMMENTEDITWARN_ENABLED` environment variable must be set to `1`.

**Configuration**: The message added to the affected issue can be specified as markdown text in environment variable `AHK_COMMENTEDITWARN_MESSAGE`.

### Single pull request for submitting work

1. There is no more than one pull request opened at any time.
1. No pull request can be opened once a PR has been closed by the teacher.

Explanation: An open pull requests is a submission of the work. There can only be one submission. A submission can be revoked by closing the pull requests. If a submission has already been evaluated (the PR has been closed by the teacher), no more submission (new PRs) are allowed.

These rules are ensured by registering such an event and adding a comment into the affected PR issue threads.

**Setup**: In order to enable this rule, the _Pull requests_ GitHub webhook trigger must be enabled and the `AHK_ONEPULLREQUEST_ENABLED` environment variable must be set to `1`.

**Configuration**: The message added to the affected issue can be specified as markdown text in environment variable `AHK_ONEPULLREQUEST_MESSAGE`. (Affected PR numbers are added to this text.)
