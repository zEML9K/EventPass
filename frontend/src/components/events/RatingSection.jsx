export default function RatingSection({ rated, rating, setRating, comment, setComment, busy, error, onSubmit }) {
  if (rated) {
    return <p className="alert success section-gap">Ви вже оцінили цю подію.</p>;
  }

  return (
    <form className="card form section-gap" onSubmit={onSubmit}>
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
      {error && <p className="alert error">{error}</p>}
      <button disabled={busy}>{busy ? "Надсилаємо…" : "Надіслати оцінку"}</button>
    </form>
  );
}