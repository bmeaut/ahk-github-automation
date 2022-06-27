# Ahk GitHub Automation

**Ahk** stands for automated homework evaluation. This repository contains tools built for enabling automated homework evaluation with the help of GitHub, GitHub Classroom, GitHub Actions. Custom tools running in containers and in Azure serverless services enable management of homework submission, evaluation and grading.

Please refer to <https://akosdudas.github.io/automated-homework-evaluation/> for the concept and details.

## Applications

**[GitHub Monitor](./github-monitor)**: An Azure function written in .NET with an http webhook registered as a GitHub Application that manages the workflow of homework submissions. Performs automatic actions on repositories acting as submissions and monitors proper usage of pull requests.

**[Publish Results to PR](./publish-results-pr)**: A [containerized](https://github.com/users/akosdudas/packages/container/package/ahk-publish-results-pr) Go application that processes the output of evaluator applications and publishes the results into a pull request for the student to see, as well as forwarding it to the _grade management_ application.

**[Grade Management](./grade-management)**: An Azure function written in .NET that accepts events from the other applications and stores them in Azure CosmosDB database. Helps teachers by reducing the administration of tracking the status of submissions and exporting final grades.

**[Review UI](./review-ui)**: A Blazor WebAssembly-based website written in .NET for displaying the state of homework submissions and final grades for the teacher.

[![](https://mermaid.ink/img/pako:eNp9UU1vwjAM_StWTiCBdq-mSQymDWlIG2inloNpvDZiTVg-ihjlv88tqWCTtlzs5zy_5zhHkRtJIhGFxV0Jz8tMA59H5Z_CZjA4x-EQxuO7xlJOqibY06Y0ZttE1sJo5Y1NzwgiXF8LTXKvjHa9HkQcdadz2FmTk3OgtCeepL0FR1oqXQBjSY7d2rhAjQVVpP21fvRs1aCpcEuQl6gLclArhN70Zd6P_Gcv1azsoJl8BUuvgQKlN10OHYAVM9n_Jr7uQuvaf02YdhguhX4nP2lw2_ZOjauMm92ngz4bRvrbPF1SrWjP2fo8Jj8FvIHPQPYAzqMPDlDL_1YlRqIiW6GS_NnHtpIJX_J1JhJOJdptJjJ9Yl7YSfT0INu1iOQdPxyNBAZvVgedi8TbQD1pppA9q8g6fQMz6sop)](https://mermaid.live/edit#pako:eNp9UU1vwjAM_StWTiCBdq-mSQymDWlIG2inloNpvDZiTVg-ihjlv88tqWCTtlzs5zy_5zhHkRtJIhGFxV0Jz8tMA59H5Z_CZjA4x-EQxuO7xlJOqibY06Y0ZttE1sJo5Y1NzwgiXF8LTXKvjHa9HkQcdadz2FmTk3OgtCeepL0FR1oqXQBjSY7d2rhAjQVVpP21fvRs1aCpcEuQl6gLclArhN70Zd6P_Gcv1azsoJl8BUuvgQKlN10OHYAVM9n_Jr7uQuvaf02YdhguhX4nP2lw2_ZOjauMm92ngz4bRvrbPF1SrWjP2fo8Jj8FvIHPQPYAzqMPDlDL_1YlRqIiW6GS_NnHtpIJX_J1JhJOJdptJjJ9Yl7YSfT0INu1iOQdPxyNBAZvVgedi8TbQD1pppA9q8g6fQMz6sop)
