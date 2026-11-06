import { useEffect, useState } from "react";
import { api } from "../api.js";

const empty = { title: "", description: "", date: "", totalTickets: 50, venueId: "" };

export default function CreateEventView({ onCreated }) {
  const [form, setForm] = useState(empty);
  const [venues, setVenues] = useState([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .getVenues()
      .then((list) => {
        setVenues(list);
        setForm((f) => ({ ...f, venueId: f.venueId || list[0]?.id || "" }));
      })
      .catch((e) => setError(e.message));
  }, []);

  const set = (field) => (e) => setForm({ ...form, [field]: e.target.value });
  const venue = venues.find((v) => v.id === Number(form.venueId));

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      await api.createEvent({
        title: form.title,
        description: form.description,
        date: form.date ? new Date(form.date).toISOString() : "0001-01-01T00:00:00Z",
        totalTickets: Number(form.totalTickets),
        venueId: Number(form.venueId),
      });
      setForm({ ...empty, venueId: form.venueId });
      onCreated();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card form" onSubmit={submit}>
      <h2>Нова подія</h2>

      <label>
        Назва
        <input value={form.title} onChange={set("title")} maxLength={200} />
      </label>

      <label>
        Опис
        <textarea rows={3} value={form.description} onChange={set("description")} />
      </label>

      <label>
        Дата й час
        <input type="datetime-local" value={form.date} onChange={set("date")} />
      </label>

      <label>
        Локація
        <select value={form.venueId} onChange={set("venueId")} disabled={venues.length === 0}>
          {venues.length === 0 && <option value="">Локацій немає</option>}
          {venues.map((v) => (
            <option key={v.id} value={v.id}>
              {v.name} — {v.address}
            </option>
          ))}
        </select>
      </label>

      <label>
        Кількість квитків
        <input
          type="number"
          min="1"
          max={venue?.capacity}
          value={form.totalTickets}
          onChange={set("totalTickets")}
        />
      </label>
      {venue && <p className="hint">Місткість локації: {venue.capacity}</p>}

      {error && <p className="alert error">{error}</p>}
      <button disabled={busy || venues.length === 0}>{busy ? "Зберігаємо…" : "Зберегти"}</button>
    </form>
  );
}
