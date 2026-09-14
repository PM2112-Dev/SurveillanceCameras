# SurveillanceCameras

cd /Users/pdm-mac/PDM/Server/SurveillanceCameras
dotnet ef migrations add MigrationsV4 --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web


dotnet new ca-usecase -n GetStorySources -fn StorySources -ut query
dotnet new ca-usecase -n GetStorySourceById -fn StorySources -ut query

dotnet new ca-usecase --name CreateStorySource --feature-name StorySources --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateStorySource --feature-name StorySources --usecase-type command
dotnet new ca-usecase --name DeleteStorySource --feature-name StorySources --usecase-type command