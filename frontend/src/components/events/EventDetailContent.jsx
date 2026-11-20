import { useAuth } from "../../auth/AuthContext.jsx";
import { formatDate } from "../../format.js";
import BookingPanel from "../BookingPanel.jsx";
import RatingSection from "./RatingSection.jsx";
import ReportSection from "./ReportSection.jsx";
import ReviewsList from "./ReviewsList.jsx";

export default function EventDetailContent({ event: ev, isFavorite, onToggleFavorite, onUpdated, rating, report }) {
  const { role, openLogin } = useAuth();

  return (
    <>
      <div className="detail-head">
        <h2>{ev.title}</h2>
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

      {role === "Visitor" ? (
        <>
          <BookingPanel key={ev.id} event={ev} onUpdated={onUpdated} />

          <RatingSection
            rated={rating.ratedIds.has(ev.id)}
            rating={rating.rating}
            setRating={rating.setRating}
            comment={rating.comment}
            setComment={rating.setComment}
            busy={rating.busy}
            error={rating.error}
            onSubmit={rating.submit}
          />

          <ReportSection
            reported={report.reportedIds.has(ev.id) || report.success}
            category={report.category}
            setCategory={report.setCategory}
            comment={report.comment}
            setComment={report.setComment}
            busy={report.busy}
            error={report.error}
            onSubmit={report.submit}
          />
        </>
      ) : (
        <p className="alert info section-gap">
          {role ? (
            "Бронювання, відгуки та скарги доступні лише з роллю «Відвідувач»."
          ) : (
            <>
              Щоб бронювати квитки, залишати відгуки та скарги,{" "}
              <button type="button" className="link" onClick={openLogin}>
                увійдіть
              </button>{" "}
              як відвідувач.
            </>
          )}
        </p>
      )}

      <ReviewsList reviews={rating.reviews} />
    </>
  );
}