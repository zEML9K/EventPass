import { useState } from "react";
import { useAuth } from "../auth/AuthContext.jsx";
import { ROLE_LABELS, ROLE_OPTIONS } from "../auth/roles.js";

export default function AuthModal() {
  const { modalOpen, notice, closeLogin, login, register } = useAuth();
  const [mode, setMode] = useState("login");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState("Visitor");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  if (!modalOpen) return null;

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      await (mode === "login" ? login : register)(username.trim(), password, role);
      setPassword("");
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  const isLogin = mode === "login";

  return (
    <div className="modal-overlay" onClick={closeLogin}>
      <form className="detail modal form narrow" onClick={(e) => e.stopPropagation()} onSubmit={submit}>
        <button type="button" className="modal-close" onClick={closeLogin}>
          &times;
        </button>
        <h2>{isLogin ? "Вхід" : "Реєстрація"}</h2>

        {notice && <p className="alert error">{notice}</p>}

        <label>
          Логін
          <input value={username} onChange={(e) => setUsername(e.target.value)} maxLength={50} autoFocus autoComplete="username" />
        </label>
        <label>
          Пароль
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            maxLength={100}
            autoComplete={isLogin ? "current-password" : "new-password"}
          />
        </label>
        <label>
          Роль
          <select value={role} onChange={(e) => setRole(e.target.value)}>
            {ROLE_OPTIONS.map((r) => (
              <option key={r} value={r}>
                {ROLE_LABELS[r]}
              </option>
            ))}
          </select>
        </label>
        {!isLogin && <p className="hint">Логін: 3–50 символів, пароль: від 6 символів.</p>}

        {error && <p className="alert error">{error}</p>}
        <button disabled={busy || !username.trim() || !password}>
          {busy ? "Зачекайте…" : isLogin ? "Увійти" : "Зареєструватися"}
        </button>

        <p className="muted section-gap">
          {isLogin ? "Немає акаунту? " : "Вже є акаунт? "}
          <button
            type="button"
            className="link"
            onClick={() => {
              setMode(isLogin ? "register" : "login");
              setError("");
            }}
          >
            {isLogin ? "Зареєструватися" : "Увійти"}
          </button>
        </p>
      </form>
    </div>
  );
}