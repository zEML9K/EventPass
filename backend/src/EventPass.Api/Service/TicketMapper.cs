using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Services;

public static class TicketMapper
{
    public static TicketDto ToDto(QrTicket t, IQrCodeService? qr, bool withQr = true) =>
        new(t.Id, t.Token, t.Status.ToString(), withQr ? qr?.GenerateBase64Png(t.Token) : null);
}