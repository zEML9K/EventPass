import { useEffect, useMemo, useState } from "react";
import { api } from "../api.js";
import { formatDate } from "../format.js";
import BookingPanel from "./BookingPanel.jsx";

const CATEGORIES = ["Кіно", "Концерт", "Театр", "Лекція"];
const REPORT_CATEGORIES = [
  "Неправдивий опис",
  "Проблема з локацією/часом",
  "Спам / неадекватний контент",
  "Інше"
];

const RATED_KEY = "eventpass_rated_events";
const REPORTED_KEY = "eventpass_reported_events";
const FAVORITES_KEY = "eventpass_favorite_events";

function loadLocalStorageSet(key) {
  try {
    return new Set(JSON.parse(localStorage.getItem(key) || "[]"));
  } catch {
    return new Set();
  }
}

function saveLocalStorageSet(key, eventId) {
  const next = loadLocalStorageSet(key);
  next.add(eventId);
  localStorage.setItem(key, JSON.stringify([...next]));
  return next;
}

export default function EventsView({ initialFavoriteFilter = false }) {
  const [events, setEvents] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [query, setQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Modal state for favorites view
  const [modalEventId, setModalEventId] = useState(null);

  // Favorites
  const [favoriteIds, setFavoriteIds] = useState(() => loadLocalStorageSet(FAVORITES_KEY));

  // Event ratings
  const [reviews, setReviews] = useState([]);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [rateBusy, setRateBusy] = useState(false);
  const [rateError, setRateError] = useState("");
  const [ratedIds, setRatedIds] = useState(() => loadLocalStorageSet(RATED_KEY));

  // Event reports
  const [reportCategory, setReportCategory] = useState(REPORT_CATEGORIES[0]);
  const [reportComment, setReportComment] = useState("");
  const [reportBusy, setReportBusy] = useState(false);
  const [reportError, setReportError] = useState("");
  const [reportSuccess, setReportSuccess] = useState(false);
  const [reportedIds, setReportedIds] = useState(() => loadLocalStorageSet(REPORTED_KEY));

  useEffect(() => {
    setLoading(true);
    setError("");
    api
      .getEvents("")
      .then((list) => {
        setEvents(list);
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const cat = selectedCategory.trim().toLowerCase();

    return events.filter((e) => {
      if (initialFavoriteFilter) {
        return favoriteIds.has(e.id);
      }

      const matchesTitle = !q || e.title.toLowerCase().includes(q);
      const matchesCategory = !cat || (e.category ?? "").toLowerCase() === cat;
      return matchesTitle && matchesCategory;
    });
  }, [events, query, selectedCategory, favoriteIds, initialFavoriteFilter]);

  const activeDetailId = initialFavoriteFilter ? modalEventId : selectedId;

  useEffect(() => {
    if (!activeDetailId) {
      setReviews([]);
      setReportSuccess(false);
      return;
    }
    setRateError("");
    setReportError("");
    setReportSuccess(false);
    api
      .getEventReviews(activeDetailId)
      .then(setReviews)
      .catch(() => setReviews([]));
  }, [activeDetailId]);

  function toggleFavorite(eventId, e) {
    e.stopPropagation();
    const next = new Set(favoriteIds);
    if (next.has(eventId)) {
      next.delete(eventId);
    } else {
      next.add(eventId);
    }
    setFavoriteIds(next);
    localStorage.setItem(FAVORITES_KEY, JSON.stringify([...next]));
  }

  const selected = filtered.find((e) => e.id === activeDetailId);

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
      setRatedIds(saveLocalStorageSet(RATED_KEY, selected.id));
      const freshReviews = await api.getEventReviews(selected.id);
      setReviews(freshReviews);
    } catch (err) {
      if (err.code === "already_rated" || err.status === 409) {
        setRatedIds(saveLocalStorageSet(RATED_KEY, selected.id));
      } else {
        setRateError(err.message);
      }
    } finally {
      setRateBusy(false);
    }
  }

  async function submitReport(e) {
    e.preventDefault();
    if (!selected) return;
    setReportBusy(true);
    setReportError("");
    try {
      await api.reportEvent(selected.id, reportCategory, reportComment);
      setReportSuccess(true);
      setReportComment("");
      setReportedIds(saveLocalStorageSet(REPORTED_KEY, selected.id));
    } catch (err) {
      if (err.code === "already_reported" || err.status === 409) {
        setReportedIds(saveLocalStorageSet(REPORTED_KEY, selected.id));
        setReportSuccess(true);
      } else {
        setReportError(err.message);
      }
    } finally {
      setReportBusy(false);
    }
  }

  const renderDetailContent = (ev) => (
    <>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
        <h2>{ev.title}</h2>
        <button
          type="button"
          onClick={(e) => toggleFavorite(ev.id, e)}
          title={favoriteIds.has(ev.id) ? "Видалити з обраного" : "Додати в обране"}
          style={{
            background: "transparent",
            border: "none",
            cursor: "pointer",
            fontSize: "1.6rem",
            padding: 0,
            lineHeight: 1,
            color: favoriteIds.has(ev.id) ? "#f59e0b" : "var(--muted)"
          }}
        >
          {favoriteIds.has(ev.id) ? "★" : "☆"}
        </button>
      </div>
      <p>{ev.description}</p>
      <dl>
        {ev.category && (
          <>
            <dt>Категорія</dt>
            <dd>{ev.category}</dd>
          </>
        )}
        <dt>Рейтинг</dt>
        <dd>
          {ev.reviewsCount > 0
            ? `★ ${ev.averageRating.toFixed(1)} (оцінок: ${ev.reviewsCount})`
            : "Ще немає оцінок"}
        </dd>
        <dt>Дата</dt>
        <dd>{formatDate(ev.date)}</dd>
        <dt>Місце</dt>
        <dd>
          {ev.venueName}
          <br />
          <span className="muted">{ev.venueAddress}</span>
        </dd>
        <dt>Вільних місць</dt>
        <dd>
          {ev.availableSeats} з {ev.totalTickets}
        </dd>
      </dl>
      <BookingPanel key={ev.id} event={ev} onUpdated={updateEvent} />

      {/* Блок оцінок */}
      {ratedIds.has(ev.id) ? (
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

      {/* Блок скарг / репортів */}
      {reportedIds.has(ev.id) || reportSuccess ? (
        <p className="alert success" style={{ marginTop: "1rem" }}>
          Скаргу надіслано. Дякуємо за допомогу!
        </p>
      ) : (
        <form className="card form" style={{ marginTop: "1rem", borderStyle: "dashed" }} onSubmit={submitReport}>
          <h4>Поскаржитися на подію</h4>
          <label>
            Причина
            <select value={reportCategory} onChange={(e) => setReportCategory(e.target.value)}>
              {REPORT_CATEGORIES.map((cat) => (
                <option key={cat} value={cat}>
                  {cat}
                </option>
              ))}
            </select>
          </label>
          <label>
            Деталі (необов'язково)
            <input
              value={reportComment}
              onChange={(e) => setReportComment(e.target.value)}
              maxLength={500}
              placeholder="Опишіть проблему..."
            />
          </label>
          {reportError && <p className="alert error">{reportError}</p>}
          <button type="submit" disabled={reportBusy} style={{ background: "#c53030" }}>
            {reportBusy ? "Надсилаємо…" : "Надіслати скаргу"}
          </button>
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
  );

  return (
    <>
      <div className={!initialFavoriteFilter && selected ? "split" : ""}>
        <section className="list-col">
          {!initialFavoriteFilter && (
            <>
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
            </>
          )}

          {loading && <p className="muted">Завантаження…</p>}
          {error && <p className="alert error">{error}</p>}
          {!loading && !error && filtered.length === 0 && (
            <p className="muted">
              {initialFavoriteFilter
                ? "У вашому обраному поки немає подій."
                : query.trim()
                ? `За запитом «${query}» нічого не знайдено.`
                : selectedCategory
                ? `Подій у категорії «${selectedCategory}» не знайдено.`
                : "Подій поки немає."}
            </p>
          )}

          <div className="list">
            {filtered.map((ev) => {
              const isFav = favoriteIds.has(ev.id);
              const isActive = activeDetailId === ev.id;
              return (
                <article
                  key={ev.id}
                  className={isActive ? "card selectable active" : "card selectable"}
                  onClick={() => {
                    if (initialFavoriteFilter) {
                      setModalEventId(ev.id);
                    } else {
                      setSelectedId(ev.id === selectedId ? null : ev.id);
                    }
                  }}
                >
                  <div className="card-head">
                    <h3>{ev.title}</h3>
                    <div style={{ display: "flex", gap: "0.5rem", alignItems: "center" }}>
                      <button
                        type="button"
                        onClick={(e) => toggleFavorite(ev.id, e)}
                        title={isFav ? "Видалити з обраного" : "Додати в обране"}
                        style={{
                          background: "transparent",
                          border: "none",
                          cursor: "pointer",
                          fontSize: "1.25rem",
                          padding: 0,
                          lineHeight: 1,
                          color: isFav ? "#f59e0b" : "var(--muted)"
                        }}
                      >
                        {isFav ? "★" : "☆"}
                      </button>
                      <span className={ev.availableSeats > 0 ? "badge ok" : "badge off"}>
                        {ev.availableSeats > 0 ? `${ev.availableSeats} з ${ev.totalTickets}` : "Квитків немає"}
                      </span>
                    </div>
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
              );
            })}
          </div>
        </section>

        {!initialFavoriteFilter && selected && (
          <aside className="detail">
            {renderDetailContent(selected)}
          </aside>
        )}
      </div>

      {/* Модальне вікно для перегляду деталей події з вкладки "Обране" */}
      {initialFavoriteFilter && modalEventId && selected && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: "rgba(0, 0, 0, 0.7)",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            zIndex: 1000,
            padding: "16px",
            overflowY: "auto"
          }}
          onClick={() => setModalEventId(null)}
        >
          <div
            className="detail"
            style={{
              position: "relative",
              maxWidth: "600px",
              width: "100%",
              maxHeight: "90vh",
              overflowY: "auto",
              background: "var(--surface)"
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <button
              type="button"
              onClick={() => setModalEventId(null)}
              style={{
                position: "absolute",
                top: "12px",
                right: "12px",
                background: "transparent",
                border: "none",
                fontSize: "1.5rem",
                cursor: "pointer",
                color: "var(--text)",
                lineHeight: 1
              }}
            >
              &times;
            </button>
            {renderDetailContent(selected)}
          </div>
        </div>
      )}
    </>
  );
}