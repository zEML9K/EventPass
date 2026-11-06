import { useEffect, useState } from "react";
import { api } from "../api.js";
import { formatDate } from "../format.js";

const BOOKING_STATUS = { Confirmed: "Підтверджено", Cancelled: "Скасовано" };
const TICKET_STATUS = { Valid: "Дійсний", Scanned: "Використано", Cancelled: "Скасовано" };

export default function BookingsView() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [cancellingId, setCancellingId] = useState(null);

  async function cancel(booking) {
    if (!window.confirm(`Скасувати бронювання №${booking.bookingId}? Невикористані квитки стануть недійсними.`)) return;
    setCancellingId(booking.bookingId);
    setError("");
    try {
      const updated = await api.cancelBooking(booking.bookingId);
      setBookings((list) => list.map((b) => (b.bookingId === updated.bookingId ? updated : b)));
    } catch (e) {
      setError(e.message);
    } finally {
      setCancellingId(null);
    }
  }

  function load() {
    setLoading(true);
    setError("");
    api
      .getBookings()
      .then(setBookings)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  return (
    <div>
      <div className="toolbar">
        <h2>Історія бронювань</h2>
        <button className="secondary" style={{ marginTop: 0 }} onClick={load} disabled={loading}>
          Оновити
        </button>
      </div>

      {error && <p className="alert error">{error}</p>}
      {loading && <p className="muted">Завантаження…</p>}
      {!loading && !error && bookings.length === 0 && <p className="muted">Бронювань поки немає.</p>}

      <div className="bookings">
        {bookings.map((b) => (
          <details key={b.bookingId} className="card booking-card">
            <summary>
              <div>
                <h3>{b.eventTitle}</h3>
                <p className="muted">
                  Бронювання №{b.bookingId} · {formatDate(b.createdAt)} · квитків: {b.quantity}
                </p>
              </div>
              <span className={b.status === "Confirmed" ? "badge ok" : "badge off"}>
                {BOOKING_STATUS[b.status] ?? b.status}
              </span>
            </summary>

            <div className="booking-body">
              <section>
                <h4>Квитки</h4>
                <ul>
                  {b.tickets.map((t) => (
                    <li key={t.id} className="ticket-row">
                      {t.qrCodeBase64 && (
                        <img
                          className="qr-thumb"
                          src={`data:image/png;base64,${t.qrCodeBase64}`}
                          alt="QR-код квитка"
                        />
                      )}
                      <div className="ticket-info">
                        <code>{t.token}</code>
                        <span className={t.status === "Valid" ? "badge ok" : "badge warn"}>
                          {TICKET_STATUS[t.status] ?? t.status}
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              </section>

              <section>
                <h4>Зміни статусу</h4>
                <ul>
                  {b.history.map((h, i) => (
                    <li key={i}>
                      <span>
                        {h.oldStatus
                          ? `${BOOKING_STATUS[h.oldStatus]} → ${BOOKING_STATUS[h.newStatus]}`
                          : `Створено: ${BOOKING_STATUS[h.newStatus] ?? h.newStatus}`}
                      </span>
                      <span className="muted">{formatDate(h.changedAt)}</span>
                    </li>
                  ))}
                </ul>
              </section>

              {b.status === "Confirmed" && b.tickets.some((t) => t.status === "Valid") && (
                <button
                  className="danger"
                  disabled={cancellingId === b.bookingId}
                  onClick={() => cancel(b)}
                >
                  {cancellingId === b.bookingId ? "Скасовуємо…" : "Скасувати бронювання"}
                </button>
              )}
            </div>
          </details>
        ))}
      </div>
    </div>
  );
}