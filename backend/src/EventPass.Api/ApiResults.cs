using EventPass.Api.DTO;

namespace EventPass.Api;

/// <summary>Р„РґРёРЅРµ РјС–СЃС†Рµ РІС–РґРїРѕРІС–РґРЅРѕСЃС‚С– РєРѕРґ РїРѕРјРёР»РєРё -> HTTP-СЃС‚Р°С‚СѓСЃ С– РїРѕРІС–РґРѕРјР»РµРЅРЅСЏ.</summary>
public static class ApiResults
{
    private static readonly Dictionary<string, (int Status, string Message)> Map = new()
    {
        // UC-02
        ["invalid_title"]            = (400, "РќР°Р·РІР° РїРѕРґС–С— РѕР±РѕРІ'СЏР·РєРѕРІР° (РґРѕ 200 СЃРёРјРІРѕР»С–РІ)."),
        ["invalid_description"]      = (400, "РћРїРёСЃ РїРѕРґС–С— РѕР±РѕРІ'СЏР·РєРѕРІРёР№."),
        ["invalid_date"]             = (400, "Р”Р°С‚Р° РїРѕРґС–С— РјР°С” Р±СѓС‚Рё РІ РјР°Р№Р±СѓС‚РЅСЊРѕРјСѓ."),
        ["invalid_total_tickets"]    = (400, "РљС–Р»СЊРєС–СЃС‚СЊ РєРІРёС‚РєС–РІ РјР°С” Р±СѓС‚Рё Р±С–Р»СЊС€РѕСЋ Р·Р° РЅСѓР»СЊ."),
        ["venue_not_found"]          = (404, "Р›РѕРєР°С†С–СЋ РЅРµ Р·РЅР°Р№РґРµРЅРѕ."),
        ["tickets_exceed_capacity"]  = (409, "РљС–Р»СЊРєС–СЃС‚СЊ РєРІРёС‚РєС–РІ РїРµСЂРµРІРёС‰СѓС” РјС–СЃС‚РєС–СЃС‚СЊ Р»РѕРєР°С†С–С—."),

        // UC-01 / UC-03
        ["event_not_found"]          = (404, "РџРѕРґС–СЋ РЅРµ Р·РЅР°Р№РґРµРЅРѕ."),
        ["invalid_quantity"]         = (400, "РљС–Р»СЊРєС–СЃС‚СЊ РєРІРёС‚РєС–РІ РјР°С” Р±СѓС‚Рё Р±С–Р»СЊС€РѕСЋ Р·Р° РЅСѓР»СЊ."),
        ["booking_not_found"]        = (404, "Р‘СЂРѕРЅСЋРІР°РЅРЅСЏ РЅРµ Р·РЅР°Р№РґРµРЅРѕ."),
        ["booking_already_cancelled"] = (409, "Р‘СЂРѕРЅСЋРІР°РЅРЅСЏ РІР¶Рµ СЃРєР°СЃРѕРІР°РЅРѕ."),
        ["booking_not_cancellable"]  = (409, "РЎРєР°СЃСѓРІР°С‚Рё РЅРµРјРѕР¶Р»РёРІРѕ: СѓСЃС– РєРІРёС‚РєРё РІР¶Рµ РІРёРєРѕСЂРёСЃС‚Р°РЅРѕ."),
        ["insufficient_seats"]       = (409, "РќРµРґРѕСЃС‚Р°С‚РЅСЊРѕ РІС–Р»СЊРЅРёС… РјС–СЃС†СЊ."),

        // UC-04
        ["invalid_token"]            = (400, "РўРѕРєРµРЅ РєРІРёС‚РєР° РЅРµ РјРѕР¶Рµ Р±СѓС‚Рё РїРѕСЂРѕР¶РЅС–Рј."),
        ["ticket_not_found"]         = (404, "РљРІРёС‚РѕРє РЅРµ Р·РЅР°Р№РґРµРЅРѕ."),
        ["ticket_already_used"]      = (409, "РљРІРёС‚РѕРє РІР¶Рµ РІРёРєРѕСЂРёСЃС‚Р°РЅРѕ."),
        ["ticket_cancelled"]         = (409, "РљРІРёС‚РѕРє СЃРєР°СЃРѕРІР°РЅРѕ."),
        // Event ratings
        ["invalid_rating"]           = (400, "Оцінка має бути від 1 до 5."),
        ["invalid_review_comment"]   = (400, "Коментар не може перевищувати 300 символів."),
        ["already_rated"]            = (409, "Ви вже залишили оцінку для цієї події."),
    };

    public static IResult FromError(string? code)
    {
        if (code != null && Map.TryGetValue(code, out var e))
            return Results.Json(new ApiError(code, e.Message), statusCode: e.Status);

        return Results.Json(new ApiError(code ?? "invalid_request", "РќРµРєРѕСЂРµРєС‚РЅРёР№ Р·Р°РїРёС‚."), statusCode: 400);
    }
}

