# SurveillanceCameras

cd /Users/pdm-mac/PDM/Server/SurveillanceCameras
dotnet ef migrations add MigrationsV5 --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web


dotnet new ca-usecase -n GetPromptTypes -fn PromptTypes -ut query
dotnet new ca-usecase -n GetPromptTypeById -fn PromptTypes -ut query

dotnet new ca-usecase --name CreatePromptType --feature-name PromptTypes --usecase-type command --return-type int
dotnet new ca-usecase --name UpdatePromptType --feature-name PromptTypes --usecase-type command
dotnet new ca-usecase --name DeletePromptType --feature-name PromptTypes --usecase-type command