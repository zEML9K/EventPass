using EventPass.Api.DTO;
using EventPass.Api.Repositories;
using EventPass.Api.Rules;

namespace EventPass.Api.Services;

/// <summary>
/// Фасад для операцій залучення аудиторії до події (відгуки/оцінки та скарги).
/// Узгоджує перевірку правил, нормалізацію ідентифікаторів клієнта та звернення до репозиторію.
/// </summary>
public sealed class EventEngagementFacade
{
    private readonly IEventRepository _repo;

    public EventEngagementFacade(IEventRepository repo) => _repo = repo;

    public Task<List<EventReviewDto>> GetReviewsAsync(int eventId) =>
        _repo.GetReviewsAsync(eventId);

    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> RateEventAsync(
        int eventId,
        RateEventRequest request,
        string? clientIp = null)
    {
        var (isValid, comment, errorCode) = EventRatingRule.Validate(request.Rating, request.Comment);
        if (!isValid)
            return (false, errorCode, null);

        var existing = await _repo.GetEventByIdAsync(eventId);
        if (existing == null)
            return (false, "event_not_found", null);

        var (newAvg, newCount) = EventRatingRule.CalculateNewRating(
            existing.AverageRating, existing.ReviewsCount, request.Rating);

        var normalizedClientId = ClientIdentityNormalizer.Normalize(request.ClientId);
        var normalizedIp = ClientIdentityNormalizer.Normalize(clientIp);

        return await _repo.AddReviewAsync(
            eventId,
            request.Rating,
            comment,
            newAvg,
            newCount,
            normalizedClientId,
            normalizedIp);
    }

public async Task<(bool Success, string? ErrorCode)> ReportEventAsync(
        int eventId,
        CreateReportRequest request,
        string? clientIp = null)
    {
        var category = request.Category?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(category) || category.Length > 100)
            return (false, "invalid_report_category");

        var comment = request.Comment?.Trim() ?? string.Empty;
        if (comment.Length > 500)
            return (false, "invalid_report_comment");

        var normalizedClientId = ClientIdentityNormalizer.Normalize(request.ClientId);
        var normalizedIp = ClientIdentityNormalizer.Normalize(clientIp);

        return await _repo.AddReportAsync(
            eventId,
            category,
            comment,
            normalizedClientId,
            normalizedIp);
    }
    public Task<List<UserReviewDto>> GetUserReviewsAsync(string clientId) =>
    _repo.GetReviewsByClientAsync(clientId);

    public Task<List<UserReportDto>> GetUserReportsAsync(string clientId) =>
        _repo.GetReportsByClientAsync(clientId);
}