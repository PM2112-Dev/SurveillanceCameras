using SurveillanceCameras.Application.WeatherForecasts.Queries.GetWeatherForecasts;
using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Service.WeatherForecasts.Queries.GetWeatherForecasts;

namespace SurveillanceCameras.Web.Endpoints;

public class WeatherForecasts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetWeatherForecasts);
    }

    [EndpointSummary("Get Weather Forecasts")]
    [EndpointDescription("Retrieves a list of weather forecasts for the next few days.")]
    public static async Task<Ok<IEnumerable<WeatherForecast>>> GetWeatherForecasts(ISender sender)
    {
        var forecasts = await sender.Send(new GetWeatherForecastsQuery());

        return TypedResults.Ok(forecasts);
    }
}
