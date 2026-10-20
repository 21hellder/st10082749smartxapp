using SmartX.Api.Services;

namespace SmartX.Api.Endpoints;

public static class RecommendationEndpoints
{
    public static void MapRecommendationEndpoints(
        this WebApplication app)
    {
        app.MapGet("/api/recommendations", (
            RecommendationEngineService engine) =>
        {
            return Results.Ok(new
            {
                success = true,
                recommendations = engine.GetRecommendations()
            });
        });

        app.MapPost("/api/recommendations/search", 
            ( SearchRequest request,
            RecommendationEngineService engine) =>
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Search term is required."
                });
            }

            engine.RecordSearch(request.SearchTerm);

            return Results.Ok(new
            {
                success = true,
                message = "Search pattern recorded."
            });
        });
    }

    public sealed class SearchRequest
    {
        public string SearchTerm { get; set; } = string.Empty;
    }
}