# Publish Results to PR

Posts the results of an automated evaluation of a student homework as a comment into a pull request and sends it to the _grade management_ application.

The application is a Go container to be used as part of a GitHub Action workflow. A previous step in the workflow is supposed to evaluate the student submission and a step using this application can process it and publish it in readable and manageable format.

## Usage of the action

The action must execute in GitHub Action within the scope of a pull request.

The action reads various environment variables automatically set by GitHub Actions to get the context, such as the repository name and pull request number.

### Arguments

#### `GITHUB_TOKEN`

**Required** A GitHub access token to work with. Generally the token provided by GitHub Actions is fine.

#### `AHK_RESULTFILE`

The path of the input file containing the evaluation result. When specified, the parsed results will be included in the result comment. Defaults to `"result.txt"`.

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
          AHK_RESULTFILE: "result.txt"
          AHK_IMAGEEXT: ".png"
          AHK_APPURL: "https://myaddress.azurewebsites.net/api/webhook-url"
          AHK_APPTOKEN: "${{ secrets.AHK_APPTOKEN }}"
          AHK_APPSECRET: "${{ secrets.AHK_APPSECRET }}"
          GITHUB_TOKEN: "${{ secrets.GITHUB_TOKEN }}"
```

## Format of the processed files

There are three types of file inputs, all taken from the repository root:

- `neptun.txt` (**mandatory**);
- `result.txt` (or other file name configured as argument of the action);
- and image files.

### Example

Let the input be as follows.

1. `neptun.txt` has a single line with content "ABC123."
1. `result.txt` text file with content:

   ```
   ###ahk#Exercise 1#2#ok
   ###ahk#Exercise 2#3#3/4 points: not all cases covered
   ###ahk#Exercise 3#0#Unexpected exception:\
   System.NotImplementedException\
   The implementation is missing
   ###ahk#optional@Exercise 4#3
   ```

1. And two image files, `image1.png` and `image2.png` are in the repository root.

These files along with the configuration of the action as above yields the following comment in the pull request:

> \<image1.png inline\>
>
> \<image2.png inline\>
>
> **Neptun**: ABC123
>
> **Exercise 1**: 2
>
> ok
>
> **Exercise 2**: 3
>
> 3/4 points: not all cases covered
>
> **Exercise 3**: 0
>
> Unexpected exception:
>
> System.NotImplementedException
>
> The implementation is missing
>
> **Total**:
>
> 5
>
> optional: 3

### Syntax of `result.txt` file

Every line in the file contains the evaluation result of one exercise as:

```
###ahk#taskname#result#comment
```

#### `###ahk#`

`###ahk#` is a mandatory prefix.

#### `taskname`

A text that identifies the task or exercise name, e.g., "Exercise 2".

Exercises can be "grouped," e.g., to distinguish optional tasks from mandatory ones. The group is optional and is part of the exercise name as "optional@Exercise 5". The total of the exercises (e.g., the cumulative points) are summed for each group separately. If there is no group in an exercise name, e.g., "Task 2", it belongs to a default group with no name.

#### `result`

A number as text that corresponds to the result (i.e., score) achieved for the particular task. If there are no scores, you can use 1 for pass and 0 for failure.

#### `comment`

An optional text interpreted as a comment. Use it to comment on the achieved result and to report problems and errors. The comment can have multiple lines; check the example above.
