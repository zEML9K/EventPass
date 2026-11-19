import { formatDate } from "../../format.js";

export default function ReviewsList({ reviews }) {
  if (reviews.length === 0) return null;

  return (
    <div className="card section-gap">
      <h4>Відгуки ({reviews.length})</h4>
      {reviews.map((r) => (
        <div key={r.id} className="review-item">
          <strong>★ {r.rating}/5</strong>{" "}
          <span className="muted">{formatDate(r.createdAt)}</span>
          {r.comment && <p className="review-comment">{r.comment}</p>}
        </div>
      ))}
    </div>
  );
}