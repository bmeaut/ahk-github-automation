# Publish Result

Posts the result of an automated evaluation of a student homework as a comment into a pull request and sends it to the _grade management_ application.

The application is a .NET container to be used as part of a GitHub Action workflow. A previous step in the workflow is supposed to evaluate the student submission and a step using this application can process it and publish it in readable and manageable format.

## Usage of the action

The action must execute in GitHub Action within the scope of a pull request.

The action reads various environment variables automatically set by GitHub Actions to get the context, such as the repository name and pull request number.

### Arguments

#### `GITHUB_TOKEN`

**Required** A GitHub access token to work with. Generally the token provided by GitHub Actions is fine.

#### `AHK_RESULTFILE`

The path of the input file containing the evaluation result. When specified, the parsed results will be included in the result comment. Defaults to `"result.json"`.

#### `AHK_IMAGEEXT`

The extension of image files (with leading dot). When specified, the images are included in the result comment. Defaults to _not specified_.

#### `AHK_APPURL`

The URL of the _grade management_ application's webhook accepting the results for storing in a database. If not specified, publishing the data to the webhook is disabled.

#### `AHK_APPTOKEN`

The token used by the _grade management_ application's webhook to authenticate requests. The token is a simple string known to the _grade management_ application. the value is sensitive data and must be protected from being published or included in files. It is recommended to store it either as a repository- or organization secret in GitHub. If not specified, publishing the data to the webhook is disabled.

#### `AHK_APPSECRET`

The secret used to sign http requests sent to the _grade management_ application's webhook. The signature is a HMAC-SHA256 signature of the request. The secret is paired with the `AHK_APPTOKEN`. It's value must be protected from being published or included in files. It is recommended to store it either as a repository- or organization secret in GitHub. If not specified, publishing the data to the webhook is disabled.

### Sample action

```yml
on: [pull_request]

jobs:
  job1:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v2

      - name: Evaluate
        run: do-eval.sh

      - name: Publish results
        uses: docker://ghcr.io/akosdudas/ahk-publish-results-pr:v1
        with:
          AHK_RESULTFILE: "result.json"
          AHK_IMAGEEXT: ".png"
          AHK_APPURL: "https://myaddress.azurewebsites.net/api/webhook-url"
          AHK_APPTOKEN: "${{ secrets.AHK_APPTOKEN }}"
          AHK_APPSECRET: "${{ secrets.AHK_APPSECRET }}"
          GITHUB_TOKEN: "${{ secrets.GITHUB_TOKEN }}"
```

## Format of the processed files

There are three types of file inputs, all taken from the repository root:

- `neptun.txt` (**mandatory**);
- `result.json` (or other file name configured as argument of the action);
- and image files.

### Example

Let the input be as follows.

1. `neptun.txt` has a single line with content "ABC123."
1. `result.json` text file with content:

    ```
    [
      {
        "ExerciseName": "Feladat 1 - Exercise 1",
        "MaxPoint": 2,
        "GivenPoint": 0
      },
      {
        "ExerciseName": "imsc Feladat 2 - Exercise 2",
        "MaxPoint": 2,
        "GivenPoint": 0
      }
    ]
    
    ```

1. And two image files, `image1.png` and `image2.png` are in the repository root.

These files along with the configuration of the action as above yields the following comment in the pull request:

> **image1.png**  
> \<image1.png inline\>
>
> **image2.png**  
> \<image2.png inline\>
>
> **Neptun**: ABC123
>
> Feladat 1 - Exercise 1: 2 out of 2 points.  
> imsc Feladat 2 - Exercise 2: 2 out of 2 points.
>
> **Osszesites/Summary**:  
> 2 out of 2  
> imsc: 2 out of 2

### Syntax of `result.json` file

Every line in the file contains the evaluation result of exercises as:

```
[
  {
    "ExerciseName": "Feladat 1 - Exercise 1",
    "MaxPoint": 2,
    "GivenPoint": 0
  },
  {
    "ExerciseName": "imsc Feladat 2 - Exercise 2",
    "MaxPoint": 2,
    "GivenPoint": 0
  }
]

```

#### `ExerciseName`

A text that identifies the task or exercise name, e.g., "Exercise 1".  
An ExerciseName with the `imsc` prefix is an optional task.

#### `MaxPoint`

The maximum points achievable.

#### `GivenPoint`

The points achieved.

