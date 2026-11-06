using EventPass.Api.DTO;

namespace EventPass.Api;

/// <summary>Єдине місце відповідності код помилки -> HTTP-статус і повідомлення.</summary>
public static class ApiResults
{
    private static readonly Dictionary<string, (int Status, string Message)> Map = new()
    {
        // UC-02
        ["invalid_title"]            = (400, "Назва події обов'язкова (до 200 символів)."),
        ["invalid_description"]      = (400, "Опис події обов'язковий."),
        ["invalid_date"]             = (400, "Дата події має бути в майбутньому."),
        ["invalid_total_tickets"]    = (400, "Кількість квитків має бути більшою за нуль."),
        ["venue_not_found"]          = (404, "Локацію не знайдено."),
        ["tickets_exceed_capacity"]  = (409, "Кількість квитків перевищує місткість локації."),

        // UC-01 / UC-03
        ["event_not_found"]          = (404, "Подію не знайдено."),
        ["invalid_quantity"]         = (400, "Кількість квитків має бути більшою за нуль."),
        ["booking_not_found"]        = (404, "Бронювання не знайдено."),
        ["booking_already_cancelled"] = (409, "Бронювання вже скасовано."),
        ["booking_not_cancellable"]  = (409, "Скасувати неможливо: усі квитки вже використано."),
        ["insufficient_seats"]       = (409, "Недостатньо вільних місць."),

        // UC-04
        ["invalid_token"]            = (400, "Токен квитка не може бути порожнім."),
        ["ticket_not_found"]         = (404, "Квиток не знайдено."),
        ["ticket_already_used"]      = (409, "Квиток вже використано."),
        ["ticket_cancelled"]         = (409, "Квиток скасовано."),
    };

    public static IResult FromError(string? code)
    {
        if (code != null && Map.TryGetValue(code, out var e))
            return Results.Json(new ApiError(code, e.Message), statusCode: e.Status);

        return Results.Json(new ApiError(code ?? "invalid_request", "Некоректний запит."), statusCode: 400);
    }
}
