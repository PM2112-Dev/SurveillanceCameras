# SurveillanceCameras

cd /Users/pdm-mac/PDM/Server/SurveillanceCameras
dotnet ef migrations add MigrationsV8 --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web


dotnet new ca-usecase -n GetAIs -fn AIs -ut query -rt AIDto
dotnet new ca-usecase -n GetAIById -fn AIs -ut query -rt AIDto

dotnet new ca-usecase --name CreateAI --feature-name AIs --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateAI --feature-name AIs --usecase-type command
dotnet new ca-usecase --name DeleteAI --feature-name AIs --usecase-type command