import { useEffect, useMemo, useState } from "react";
import { api } from "../api.js";
import { formatDate } from "../format.js";
import BookingPanel from "./BookingPanel.jsx";

export default function EventsView() {
  const [events, setEvents] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .getEvents()
      .then(setEvents)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return events;
    return events.filter((e) => e.title.toLowerCase().includes(q));
  }, [events, query]);

  const selected = events.find((e) => e.id === selectedId);

  const updateEvent = (updated) =>
    setEvents((list) => list.map((e) => (e.id === updated.id ? updated : e)));

  if (loading) return <p className="muted">Завантаження…</p>;
  if (error) return <p className="alert error">{error}</p>;
  if (events.length === 0) return <p className="muted">Подій поки немає.</p>;

  return (
    <div className="split">
      <section className="list-col">
        <input
          className="search"
          type="search"
          placeholder="Пошук події за назвою…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />

        <div className="list">
          {filtered.length === 0 && <p className="muted">За запитом «{query}» нічого не знайдено.</p>}

          {filtered.map((ev) => (
            <article
              key={ev.id}
              className={ev.id === selectedId ? "card selectable active" : "card selectable"}
              onClick={() => setSelectedId(ev.id)}
            >
              <div className="card-head">
                <h3>{ev.title}</h3>
                <span className={ev.availableSeats > 0 ? "badge ok" : "badge off"}>
                  {ev.availableSeats > 0 ? `${ev.availableSeats} з ${ev.totalTickets}` : "Квитків немає"}
                </span>
              </div>
              <p className="muted">{formatDate(ev.date)}</p>
              <p className="muted">{ev.venueName}</p>
            </article>
          ))}
        </div>
      </section>

      <aside className="detail">
        {selected ? (
          <>
            <h2>{selected.title}</h2>
            <p>{selected.description}</p>
            <dl>
              <dt>Дата</dt>
              <dd>{formatDate(selected.date)}</dd>
              <dt>Місце</dt>
              <dd>
                {selected.venueName}
                <br />
                <span className="muted">{selected.venueAddress}</span>
              </dd>
              <dt>Вільних місць</dt>
              <dd>
                {selected.availableSeats} з {selected.totalTickets}
              </dd>
            </dl>
            <BookingPanel key={selected.id} event={selected} onUpdated={updateEvent} />
          </>
        ) : (
          <p className="muted">Оберіть подію зі списку.</p>
        )}
      </aside>
    </div>
  );
}
