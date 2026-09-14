# SurveillanceCameras

The project was generated using the [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) version 10.8.0.

## Build

Run `dotnet build` to build the solution.

## Run

To run the application:

```bash
dotnet run --project .\src\AppHost
```

The Aspire dashboard will open automatically, showing the application URLs and logs.

## JWT local setup

The API now uses bearer tokens (JWT). For local development, set a strong signing key with user secrets:

```bash
cd ./src/Web
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "replace-with-your-very-long-random-secret-key"
```

You can also override `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:AccessTokenExpirationMinutes` the same way if needed.

## Code Styles & Formatting

The template includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs. The **.editorconfig** file defines the coding styles applicable to this solution.

## Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the `.\src\Application\` folder.

Create a new command:

```
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

Create a new query:

```
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If you encounter the error *"No templates or subcommands found matching: 'ca-usecase'."*, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.8.0
```

## Test

The solution contains unit, integration, and functional tests.

To run the tests:
```bash
dotnet test
```

## Help
To learn more about the template go to the [project website](https://cleanarchitecture.jasontaylor.dev). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.

cd /Users/pdm-mac/PDM/Server/SurveillanceCameras
dotnet ef migrations add MigrationsV3 --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web


{
"tokenType": "Bearer",
"accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1OTBmZmVkNS1kODI1LTQ2ZTUtYmZmOC03MWM1Yzc3MGIyMzAiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjU5MGZmZWQ1LWQ4MjUtNDZlNS1iZmY4LTcxYzVjNzcwYjIzMCIsImVtYWlsIjoiYWRtaW5pc3RyYXRvckBsb2NhbGhvc3QiLCJ1bmlxdWVfbmFtZSI6ImFkbWluaXN0cmF0b3JAbG9jYWxob3N0IiwianRpIjoiZTg1ODNjMDQtZDMwNy00MGU2LWI1NmItZTdjZGVjNjg2NDA5IiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW5pc3RyYXRvciIsImV4cCI6MTc3NjAwMjA5MywiaXNzIjoiU3VydmVpbGxhbmNlQ2FtZXJhcy5XZWIiLCJhdWQiOiJTdXJ2ZWlsbGFuY2VDYW1lcmFzLkNsaWVudCJ9.NIuUBTFIVVFRo-XiEg1BeG2goUQ3h2toiMaTsg1opNs",
"expiresIn": 3599,
"refreshToken": "6Djj1ObAzFoukl3K2fnYLVo2tpwFPLot0dDkgDRmgHxc7thbuz3QWISVdVEVUMuf951l0oVGBMQHEadpahhGDg=="
}

dotnet new ca-usecase -n GetStories -fn Stories -ut query

dotnet new ca-usecase --name CreateStory --feature-name Stories --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateStory --feature-name Stories --usecase-type command
dotnet new ca-usecase --name DeleteStory --feature-name Stories --usecase-type command