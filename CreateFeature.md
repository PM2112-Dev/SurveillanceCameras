# SurveillanceCameras

cd /Users/pdm-mac/PDM/Server/SurveillanceCameras
dotnet ef migrations add MigrationsV9 --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web


dotnet new ca-usecase -n GetStoryVideoTypes -fn StoryVideoTypes -ut query -rt StoryVideoTypeDto
dotnet new ca-usecase -n GetStoryVideoTypeById -fn StoryVideoTypes -ut query -rt StoryVideoTypeDto

dotnet new ca-usecase --name CreateStoryVideoType --feature-name StoryVideoTypes --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateStoryVideoType --feature-name StoryVideoTypes --usecase-type command
dotnet new ca-usecase --name DeleteStoryVideoType --feature-name StoryVideoTypes --usecase-type command