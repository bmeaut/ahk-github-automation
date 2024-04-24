# Incoming Event Scenarios

## Scenario 1: Student accepts assignment

### Description

A student accepts an assignment. Repository is created and grade management system is notified through gitHub monitor.

Incoming Data:

- Assignment GitHub Repo Url
    - Excercise GitHub prefix is included so excercise can be identified
    - Student GitHubId is included so student can be identified
    - Subject GitHub Organiztation Name is included so subject can be identified if needed

## Scenario 2/1: Student opens a pull request

### Description

A student submits an assignment. Grade management system is notified through gitHub monitor.

Incoming Data:

- Assignment GitHub Repo Url
- Pull Request Url
- Pull Request Opening Date/Time
- Pull Request branch name

## Scenario 2/2: If automated grading is enabled points get calculated

### Description

If automated grading is enabled, grade management system gets points from GitHub Actions for the assignment.

Incoming Data:

- Assignment GitHub Repo Url
- Pull Request Url
- Points
- Points Type
- Date of points calculation

## Scenario 3: Student assigns teacher a.k.a. submits assignment

### Description

A student submits an assignment with assigning a teacher. Grade management system is notified through gitHub monitor.

Incoming Data:

- Assignment GitHub Repo Url
- Assigned teachers gitHubId
- Pull Request Url

## Scenario 4: Teacher approves / overrides points

### Description

Teacher approves or overrides points for an assignment. Grade management system is notified through gitHub monitor.
If there is a change in points, new points get registered and also approved otherwise latest already existing points get
approved.
Pull request is closed.

Incoming Data:

- Assignment GitHub Repo Url
- Pull Request Url
- Teachers gitHubId
- Points in order (type1, type2, ...)
- Date of approval


