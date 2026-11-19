import { REPORT_CATEGORIES } from "./hooks/constants.js";

export default function ReportSection({ reported, category, setCategory, comment, setComment, busy, error, onSubmit }) {
  if (reported) {
    return <p className="alert success section-gap">Скаргу надіслано. Дякуємо за допомогу!</p>;
  }

  return (
    <form className="card form form-dashed section-gap" onSubmit={onSubmit}>
      <h4>Поскаржитися на подію</h4>
      <label>
        Причина
        <select value={category} onChange={(e) => setCategory(e.target.value)}>
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
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          maxLength={500}
          placeholder="Опишіть проблему..."
        />
      </label>
      {error && <p className="alert error">{error}</p>}
      <button type="submit" className="btn-report" disabled={busy}>
        {busy ? "Надсилаємо…" : "Надіслати скаргу"}
      </button>
    </form>
  );
}