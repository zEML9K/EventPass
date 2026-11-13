import { useEffect, useMemo, useState } from "react";
import { api } from "../api.js";
import { formatDate } from "../format.js";
import BookingPanel from "./BookingPanel.jsx";

// Category event and filter category
const CATEGORIES = ["Кіно", "Концерт", "Театр", "Лекція"];

export default function EventsView() {
  const [events, setEvents] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [query, setQuery] = useState("");
  // Category event and filter category
  const [selectedCategory, setSelectedCategory] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Category event and filter category
  useEffect(() => {
    setLoading(true);
    setError("");
    api
      .getEvents(selectedCategory)
      .then((list) => {
        setEvents(list);
        setSelectedId((prev) => (list.some((e) => e.id === prev) ? prev : null));
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [selectedCategory]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const cat = selectedCategory.trim().toLowerCase();

    return events.filter((e) => {
      const matchesTitle = !q || e.title.toLowerCase().includes(q);
      const matchesCategory = !cat || (e.category ?? "").toLowerCase() === cat;
      return matchesTitle && matchesCategory;
    });
  }, [events, query, selectedCategory]);

  const selected = filtered.find((e) => e.id === selectedId);

  const updateEvent = (updated) =>
    setEvents((list) => list.map((e) => (e.id === updated.id ? updated : e)));

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

        {/* Category event and filter category */}
        <select
          className="search"
          aria-label="Фільтр за категорією"
          value={selectedCategory}
          onChange={(e) => setSelectedCategory(e.target.value)}
        >
          <option value="">Усі категорії</option>
          {CATEGORIES.map((cat) => (
            <option key={cat} value={cat}>
              {cat}
            </option>
          ))}
        </select>

        {loading && <p className="muted">Завантаження…</p>}
        {error && <p className="alert error">{error}</p>}
        {!loading && !error && filtered.length === 0 && (
          <p className="muted">
            {query.trim()
              ? `За запитом «${query}» нічого не знайдено.`
              : selectedCategory
              ? `Подій у категорії «${selectedCategory}» не знайдено.`
              : "Подій поки немає."}
          </p>
        )}

        <div className="list">
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
              {/* Category event and filter category */}
              {ev.category && <p className="muted">Категорія: {ev.category}</p>}
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
              {/* Category event and filter category */}
              {selected.category && (
                <>
                  <dt>Категорія</dt>
                  <dd>{selected.category}</dd>
                </>
              )}
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