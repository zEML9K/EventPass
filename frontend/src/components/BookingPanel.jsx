import { useState } from "react";
import { api } from "../api.js";

export default function BookingPanel({ event, onUpdated }) {
  const [quantity, setQuantity] = useState(1);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [booking, setBooking] = useState(null);

  // UC-01 A1: місць немає — бронювання недоступне
  if (event.availableSeats === 0 && !booking) {
    return <p className="alert error">Бронювання недоступне: вільних квитків немає.</p>;
  }

  async function submit() {
    setBusy(true);
    setError("");
    try {
      setBooking(await api.book(event.id, quantity));
      onUpdated(await api.getEvent(event.id)); // оновлюємо лічильник місць
    } catch (e) {
      setError(e.message);
      // Місць могло побільшати/поменшати — підтягуємо актуальні дані
      api.getEvent(event.id).then(onUpdated).catch(() => {});
    } finally {
      setBusy(false);
    }
  }

  if (booking) {
    return (
      <div>
        <p className="alert success">
          Бронювання №{booking.bookingId} підтверджено. Квитків: {booking.quantity}.
        </p>
        <div className="tickets">
          {booking.tickets.map((t) => (
            <div key={t.id} className="ticket">
              {t.qrCodeBase64 && (
                <img src={`data:image/png;base64,${t.qrCodeBase64}`} alt="QR-код квитка" />
              )}
              <code>{t.token}</code>
              <button className="link" onClick={() => navigator.clipboard?.writeText(t.token)}>
                Копіювати токен
              </button>
            </div>
          ))}
        </div>
        <button className="secondary" onClick={() => setBooking(null)}>
          Забронювати ще
        </button>
      </div>
    );
  }

  return (
    <div className="booking">
      <label>
        Кількість квитків
        <input
          type="number"
          min="1"
          max={event.availableSeats}
          value={quantity}
          onChange={(e) => setQuantity(Number(e.target.value))}
        />
      </label>
      {error && <p className="alert error">{error}</p>}
      <button onClick={submit} disabled={busy || quantity < 1}>
        {busy ? "Бронюємо…" : "Забронювати"}
      </button>
    </div>
  );
}
