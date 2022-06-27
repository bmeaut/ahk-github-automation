# Review UI

This application is a website written in .NET using Blazor WebAssembly to display the status and results of homework submissions. The website runs entirely in the browser, it requires no backend, however, it fetches data via API from [Grade Management](../grade-management). To access the API an access key is required, which is a suitable [master Function Access Key](https://docs.microsoft.com/en-us/azure/azure-functions/security-concepts?tabs=v4#function-access-keys) of the latter application deployed into Azure Function.

## Global configuration of the application

The application requires the URL of the backend API to be configured in the `appsettings.json` file. To enable dynamic configuration in CI, the publish pipeline edits this file and substitutes the value of the repository secret `REVIEWUI_GRADEMANAGEMENTAPI` into this file.

This application calls the public API of [Grade Management](../grade-management). When that application is deployed to Azure Function, it will block requests from browsers due to CORS. Since this is a Blazor WebAssembly application, it runs entirely in the browser. To enable access to the API from this application, CORS must be allowed in the Azure Function of _Grade Management_ by enabling calls from the particular address this application is deployed to.
