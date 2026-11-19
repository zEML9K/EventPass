import { formatDate } from "../../format.js";

export default function EventCard({ event: ev, isFavorite, active, onToggleFavorite, onSelect }) {
  return (
    <article
      className={active ? "card selectable active" : "card selectable"}
      onClick={() => onSelect(ev.id)}
    >
      <div className="card-head">
        <h3>{ev.title}</h3>
        <div className="card-actions">
          <button
            type="button"
            className={isFavorite ? "fav-btn active" : "fav-btn"}
            onClick={(e) => onToggleFavorite(ev.id, e)}
            title={isFavorite ? "Видалити з обраного" : "Додати в обране"}
          >
            {isFavorite ? "★" : "☆"}
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
}