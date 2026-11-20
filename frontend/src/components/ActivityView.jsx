import { useEffect, useState } from "react";
import { authApi } from "../auth/authApi.js";
import { ROLE_LABELS } from "../auth/roles.js";
import { formatDate } from "../format.js";

const BOOKING_STATUS = { Confirmed: "Підтверджено", Cancelled: "Скасовано" };

export default function ActivityView() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  function load() {
    setLoading(true);
    setError("");
    authApi
      .me()
      .then(setData)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  const profile = data?.profile;
  const isVisitor = profile?.role === "Visitor";

  return (
    <div>
      <div className="toolbar">
        <h2>Мій акаунт</h2>
        <button className="secondary" style={{ marginTop: 0 }} onClick={load} disabled={loading}>
          Оновити
        </button>
      </div>

      {error && <p className="alert error">{error}</p>}
      {loading && <p className="muted">Завантаження…</p>}

      {profile && (
        <>
          <section className="card">
            <dl>
              <dt>Логін</dt>
              <dd>{profile.username}</dd>
              <dt>Роль</dt>
              <dd>{ROLE_LABELS[profile.role] ?? profile.role}</dd>
              <dt>Акаунт створено</dt>
              <dd>{formatDate(profile.createdAt)}</dd>
            </dl>
          </section>

          {isVisitor && (
            <>
              <section className="section-gap">
                <h3>Бронювання ({data.bookings.length})</h3>
                {data.bookings.length === 0 && <p className="muted">Бронювань поки немає.</p>}
                <div className="list">
                  {data.bookings.map((b) => (
                    <article key={b.bookingId} className="card">
                      <div className="card-head">
                        <h3>{b.eventTitle}</h3>
                        <span className={b.status === "Confirmed" ? "badge ok" : "badge off"}>
                          {BOOKING_STATUS[b.status] ?? b.status}
                        </span>
                      </div>
                      <p className="muted">
                        №{b.bookingId} · {formatDate(b.createdAt)} · квитків: {b.quantity}
                      </p>
                    </article>
                  ))}
                </div>
              </section>

              <section className="section-gap">
                <h3>Мої відгуки ({data.reviews.length})</h3>
                {data.reviews.length === 0 && <p className="muted">Ви ще не оцінювали події.</p>}
                <div className="list">
                  {data.reviews.map((r) => (
                    <article key={r.id} className="card">
                      <strong>{r.eventTitle}</strong>{" "}
                      <span className="muted">· ★ {r.rating}/5 · {formatDate(r.createdAt)}</span>
                      {r.comment && <p className="review-comment">{r.comment}</p>}
                    </article>
                  ))}
                </div>
              </section>

              <section className="section-gap">
                <h3>Мої скарги ({data.reports.length})</h3>
                {data.reports.length === 0 && <p className="muted">Скарг не надсилали.</p>}
                <div className="list">
                  {data.reports.map((r) => (
                    <article key={r.id} className="card">
                      <strong>{r.eventTitle}</strong>{" "}
                      <span className="muted">· {r.category} · {formatDate(r.createdAt)}</span>
                      {r.comment && <p className="review-comment">{r.comment}</p>}
                    </article>
                  ))}
                </div>
              </section>
            </>
          )}
        </>
      )}
    </div>
  );
}