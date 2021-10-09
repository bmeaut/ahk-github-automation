# Ahk GitHub Automation

**Ahk** stands for automated homework evaluation. This repository contains tools built for enabling automated homework evaluation with the help of GitHub, GitHub Classroom, GitHub Actions. Custom tools running in containers and in Azure serverless services enable management of homework submission, evaluation and grading.

Please refer to <https://akosdudas.github.io/automated-homework-evaluation/> for the concept and details.

## Applications

**[GitHub Monitor](./github-monitor)**: An Azure function written in .NET Core with an http webhook registered as a GitHub Application that manages the workflow of homework submissions. Performs automatic actions on repositories acting as submissions and monitors proper usage of pull requests.

**[Publish Results to PR](./publish-results-pr)**: A [containerized](https://github.com/users/akosdudas/packages/container/package/ahk-publish-results-pr) Go application that processes the output of evaluator applications and publishes the results into a pull request for the student to see, as well as forwarding it to the _grade management_ application.

**[Grade Management](./grade-management)**: An Azure function written in .NET Core that accepts grades from the other applications and stores them in Azure CosmosDB database. Helps teachers by reducing the administration of submissions and grades.
