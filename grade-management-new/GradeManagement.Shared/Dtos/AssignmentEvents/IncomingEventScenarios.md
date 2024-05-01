# Incoming Event Scenarios

## Scenario 1: Student accepts assignment

### Description

A student accepts an assignment. Repository is created and grade management system is notified through gitHub monitor.

Incoming Data:

- Assignment GitHub Repo Url
    - Excercise GitHub prefix is included so excercise can be identified
    - Student GitHubId is included so student can be identified
    - Subject GitHub Organiztation Name is included so subject can be identified if needed

## Scenario 2: Student opens a pull request

### Description

A student submits an assignment. Grade management system is notified through gitHub monitor.

Incoming Data:

- Assignment GitHub Repo Url
- Pull Request Url
- Pull Request Opening Date/Time
- Pull Request branch name

## Scenario 3: CI sends evaluation results

### Description

If automated grading is enabled, grade management system gets points from GitHub Actions for the assignment.

Incoming Data:

- Assignment GitHub Repo Url
- Pull Request Url
- Student Neptun Code
- Points (empty if automated grading is disabled)
- Points Type (empty if automated grading is disabled)
- Date of evaluation

## Scenario 4: Student assigns teacher a.k.a. submits assignment

### Description

A student submits an assignment with assigning a teacher. Grade management system is notified through gitHub monitor.

Incoming Data:

- Assignment GitHub Repo Url
- Assigned teachers gitHubId
- Pull Request Url

## Scenario 5: Teacher approves / overrides points

### Description

Teacher approves or overrides points for an assignment. Grade management system is notified through gitHub monitor.
If there is a change in points, new points get registered and also approved otherwise latest already existing points get
approved.

Incoming Data:

- Assignment GitHub Repo Url
- Pull Request Url
- Teachers gitHubId
- Points in order (type1, type2, ...) (empty if teacher approves latest auto generated points)
- Date of approval

## Scenario 6: Pull request gets closed

### Description

Pull request gets closed. Grade management system is notified through gitHub monitor.

Incoming Data:

- Assignment GitHub Repo Url
- Pull Request Url
