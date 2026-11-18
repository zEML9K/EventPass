import { useEffect, useMemo, useState } from "react";
import { api } from "../api.js";
import { formatDate } from "../format.js";
import BookingPanel from "./BookingPanel.jsx";

const CATEGORIES = ["Кіно", "Концерт", "Театр", "Лекція"];
const RATED_KEY = "eventpass_rated_events";

function loadRatedIds() {
  try {
    return new Set(JSON.parse(localStorage.getItem(RATED_KEY) || "[]"));
  } catch {
    return new Set();
  }
}

function saveRatedId(eventId) {
  const next = loadRatedIds();
  next.add(eventId);
  localStorage.setItem(RATED_KEY, JSON.stringify([...next]));
  return next;
}

export default function EventsView() {
  const [events, setEvents] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [query, setQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Event ratings
  const [reviews, setReviews] = useState([]);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [rateBusy, setRateBusy] = useState(false);
  const [rateError, setRateError] = useState("");
  const [ratedIds, setRatedIds] = useState(loadRatedIds);

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

  useEffect(() => {
    if (!selectedId) {
      setReviews([]);
      return;
    }
    setRateError("");
    api
      .getEventReviews(selectedId)
      .then(setReviews)
      .catch(() => setReviews([]));
  }, [selectedId]);

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

  async function submitRating(e) {
    e.preventDefault();
    if (!selected) return;
    setRateBusy(true);
    setRateError("");
    try {
      const updated = await api.rateEvent(selected.id, Number(rating), comment);
      updateEvent(updated);
      setComment("");
      setRatedIds(saveRatedId(selected.id));
      const freshReviews = await api.getEventReviews(selected.id);
      setReviews(freshReviews);
    } catch (err) {
      if (err.code === "already_rated" || err.status === 409) {
        setRatedIds(saveRatedId(selected.id));
      } else {
        setRateError(err.message);
      }
    } finally {
      setRateBusy(false);
    }
  }

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
              {ev.category && <p className="muted">Категорія: {ev.category}</p>}
              <p className="muted">
                Рейтинг:{" "}
                {ev.reviewsCount > 0
                  ? `★ ${ev.averageRating.toFixed(1)} (${ev.reviewsCount})`
                  : "ще немає оцінок"}
              </p>
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
              {selected.category && (
                <>
                  <dt>Категорія</dt>
                  <dd>{selected.category}</dd>
                </>
              )}
              <dt>Рейтинг</dt>
              <dd>
                {selected.reviewsCount > 0
                  ? `★ ${selected.averageRating.toFixed(1)} (оцінок: ${selected.reviewsCount})`
                  : "Ще немає оцінок"}
              </dd>
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

            {ratedIds.has(selected.id) ? (
              <p className="alert success" style={{ marginTop: "1rem" }}>
                Ви вже оцінили цю подію.
              </p>
            ) : (
              <form className="card form" style={{ marginTop: "1rem" }} onSubmit={submitRating}>
                <h4>Оцінити подію</h4>
                <label>
                  Оцінка (1–5)
                  <select value={rating} onChange={(e) => setRating(e.target.value)}>
                    <option value={5}>5 — Відмінно</option>
                    <option value={4}>4 — Добре</option>
                    <option value={3}>3 — Нормально</option>
                    <option value={2}>2 — Слабко</option>
                    <option value={1}>1 — Погано</option>
                  </select>
                </label>
                <label>
                  Коментар
                  <input
                    value={comment}
                    onChange={(e) => setComment(e.target.value)}
                    maxLength={300}
                    placeholder="Ваші враження (необов'язково)"
                  />
                </label>
                {rateError && <p className="alert error">{rateError}</p>}
                <button disabled={rateBusy}>{rateBusy ? "Надсилаємо…" : "Надіслати оцінку"}</button>
              </form>
            )}

            {reviews.length > 0 && (
              <div className="card" style={{ marginTop: "1rem" }}>
                <h4>Відгуки ({reviews.length})</h4>
                {reviews.map((r) => (
                  <div key={r.id} style={{ borderTop: "1px solid #eee", paddingTop: "0.5rem", marginTop: "0.5rem" }}>
                    <strong>★ {r.rating}/5</strong>{" "}
                    <span className="muted">{formatDate(r.createdAt)}</span>
                    {r.comment && <p style={{ margin: "0.25rem 0 0" }}>{r.comment}</p>}
                  </div>
                ))}
              </div>
            )}
          </>
        ) : (
          <p className="muted">Оберіть подію зі списку.</p>
        )}
      </aside>
    </div>
  );
}
