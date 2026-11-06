import { useState } from "react";
import { api } from "../api.js";

export default function ScanView() {
  const [token, setToken] = useState("");
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState(null);

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setResult(null);
    try {
      await api.checkTicket(token);
      setResult({ ok: true, text: "Квиток дійсний. Прохід дозволено." });
      setToken("");
    } catch (err) {
      setResult({ ok: false, text: err.message });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card form narrow" onSubmit={submit}>
      <h2>Перевірка квитка</h2>
      <label>
        Токен із QR-коду
        <input value={token} onChange={(e) => setToken(e.target.value)} autoFocus />
      </label>
      <button disabled={busy}>{busy ? "Перевіряємо…" : "Перевірити"}</button>
      {result && (
        <p className={result.ok ? "alert success" : "alert error"}>{result.text}</p>
      )}
    </form>
  );
}
