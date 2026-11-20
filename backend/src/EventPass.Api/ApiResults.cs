using EventPass.Api.DTO;

namespace EventPass.Api;

/// <summary>Єдине місце відповідності коду помилки -> HTTP-статус і повідомлення.</summary>
public static class ApiResults
{
    private static readonly Dictionary<string, (int Status, string Message)> Map = new()
    {
        // UC-02
        ["invalid_title"]            = (400, "Назва події обов'язкова (до 200 символів)."),
        ["invalid_description"]      = (400, "Опис події обов'язковий."),
        ["invalid_category"]         = (400, "Категорія події обов'язкова."),
        ["category_too_long"]        = (400, "Назва категорії не може перевищувати 50 символів."),
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
        ["ticket_already_used"]      = (409, "Квиток уже використано."),
        ["ticket_cancelled"]         = (409, "Квиток скасовано."),

        // Event ratings & reviews
        ["invalid_rating"]           = (400, "Оцінка має бути від 1 до 5."),
        ["invalid_review_comment"]   = (400, "Коментар не може перевищувати 300 символів."),
        ["already_rated"]            = (409, "Ви вже залишили оцінку для цієї події."),

        // Event reports
        ["invalid_report_category"]  = (400, "Категорія скарги обов'язкова."),
        ["invalid_report_comment"]   = (400, "Коментар скарги не може перевищувати 500 символів."),
        ["already_reported"]         = (409, "Ви вже надіслали скаргу щодо цієї події."),

        // Auth / roles
        ["invalid_username"]    = (400, "Логін обов'язковий (3–50 символів)."),
        ["invalid_password"]    = (400, "Пароль має містити від 6 до 100 символів."),
        ["invalid_role"]        = (400, "Невідома роль. Допустимо: Visitor, Organizer, Controller."),
        ["username_taken"]      = (409, "Користувач з таким логіном уже існує."),
        ["invalid_credentials"] = (401, "Невірний логін або пароль."),
        ["role_mismatch"]       = (403, "Обрана роль не відповідає ролі акаунту."),
        ["unauthorized"]        = (401, "Потрібна авторизація."),
        ["forbidden_role"]      = (403, "Ваша роль не має доступу до цієї дії."),
    };

    public static IResult FromError(string? code)
    {
        if (code != null && Map.TryGetValue(code, out var e))
            return Results.Json(new ApiError(code, e.Message), statusCode: e.Status);

        return Results.Json(new ApiError(code ?? "invalid_request", "Некоректний запит."), statusCode: 400);
    }
}