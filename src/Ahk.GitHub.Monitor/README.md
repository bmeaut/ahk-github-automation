# GitHub Monitor

A GitHub App hosted as an Azure Function processing GitHub webhooks. The purpose is to monitor GitHub repositories of student homework assignments and help students and teachers through the expected workflow.

The basis of operation is

1. a GitHub organization where the student assignments are stored, managed by GitHub Classroom;
1. a running instance of this application hosted as an Azure Function with a public URL;
1. a GitHub App pointing to the Azure function, added to the GitHub organization (handling events in _all_ repository of the GitHub organization);
1. and finally, the [grade management](../grade-management) application receiving teacher approval events from this application.

## ~~Public~~ GitHub App

The GitHub App built from this source is **private**, but feel free to fork the source code and create your own.

## Global configuration of the application

The application requires the following configurations specified as **environment variables**.

`AHK_GitHubAppId` and `AHK_GitHubAppPrivateKey`: (**mandatory**) the ID and the private key of the GitHub App (both available on the GitHub App management page). All GitHub interactions from the application will be performed in the name of this GitHub App.

`AHK_GitHubWebhookSecret`: (**mandatory**) the secret configured in the GitHub App. The secret is mandatory; unsecured webhook calls are rejected by the application.

`AHK_EventsQueueConnectionString`: (optional) for integration with the _grade management_ application. The value is the connection string of an Azure Queue Storage, e.g., `DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=mykey;EndpointSuffix=core.windows.net`

## Enable for a repository

The GitHub app is installed in a GitHub organization and hence receives events for all kinds of activities within that organization. To limit the scope of this application, each repository that this GitHub App should monitor must have a `.github/ahk-monitor.yml` file in the default branch with the following content

```yaml
enabled: true
```

If this file is not present, its content is not `enabled` / `enabled: true` / `enabled: yes`, all fired events are ignored for the specific repository.

## Rules enforced by the application

The application enforces the following rules in the enabled repositories.

### Branch usage

> The default branch is not touched by the student. The solution is submitted on a new branch via a pull request.
>
> There are no force pushes.

These rules are enforced by marking all branches as protected and requiring review of pull requests targeting the default branch.

### Issue comment editing

> No user edits or deletes an issue comment made by someone else.

This rule is ensured by registering such an attempt and adding a comment into the issue thread where the edit/delete took place.

### Single pull request for submitting work

> There is no more than one open pull request at any time.
>
> A pull request should not be opened once a PR has been closed by the teacher.

Explanation: An open pull request is a submission of the work. There can only be one submission. A submission can be revoked by closing the pull requests. If a submission has already been evaluated (the PR has been closed by the teacher), no more submissions (new PRs) are allowed.

These rules are ensured by registering such an event and adding a comment into the affected PR issue threads.

### Reviewer must be assigned to the pull request

> Pull requests should be assigned to the instructor for evaluation. When a review is requested instead, assign the instructor.

Explanation: To track submitted work the pull requests should be assigned to the instructor. This allows the instructor to use GitHub web interface to go through all submissions. Requesting a review of a pull request is frequently mixed up with assignment. Although GitHub has a list of requested reviews, if the instructor choses to request a change in the submission, the pull request is removed from this list. If the student makes changes to the pull request as the result of the change request, the pull request does not show up in the pending reviews list. To ensure tracking these pull requests any reviewer is automatically set as assignee too.

### No more than 5 Actions workflow run

> GitHub Actions workflows run automated evaluation on submissions. Each student has 5 evaluations after which the process is disabled.

Explanation: GitHub Actions CI is used to execute the automated evaluation. Since running these workflows count against the CI minutes a GitHub Organization has, each student is limited to 5 evaluations. When this limit is reached, first the student is warned. After another evaluation GitHub Actions is disabled for the repository.

## Approval and grading with pull request comment

Students submit their work in pull requests. Teachers check the contents and approve it (or might request changes). This application offers "chatops"-like approval using the following command entered by the teacher in the pull request as comment: `/ahk ok`. This command will trigger approval of the pull request and its content is merged into the default (`master`/`main`) branch. By merging the pull request in such a way serves two purposes:

1. The teacher can see which submissions were processed. Approved and merged pull requests require no further action.
1. It is general software development practice to submit pull requests and then have them merged. This allows the students to see this process in action.

The approval command is accepted from any member of the owner GitHub organization. As students are added by GitHub Classroom as _outside collaborators_, only teachers can execute such commands. (Any attempt made by the student is recorded as a comment in the pull request.)

### Integration with the [_grade management_](../grade-management) application

When submission requires grading (assignment of a grade or point, or multiple points) the teacher can enter points/grades which are recorded by the _grade management_ application. By entering the grades/points in the GitHub pull request there is no further administration of grades required as the grades are forwarded to the _grade management_ application and stored in a database.

Grading or point assignment is part of the approval by using the approval command the following way:

```
/ahk ok 5
/ahk ok 5 3.5 0
```

The numbers entered here correspond to exercises. The order of the numbers is kept as-is by the grade registration process and can be interpreted when exporting from the database at a later point in time.

#### Enabling integration

Approval of the pull request with the `/ahk ok` command is enabled without the integration. Grade registration, however, requires the _grade management_ application and communication with that service via an Azure Queue Storage. The access information of the queue (including access token) is read from the `AHK_EventsQueueConnectionString` optional environment variable. If this environment variable is not provided, grade registration is disabled.
