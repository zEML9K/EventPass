import { request, post } from "../api.js";
import { clearSession, loadSession, saveSession } from "./session.js";

const toSession = (r) => ({
  userId: r.userId,
  username: r.username,
  role: r.role,
  clientId: r.clientId,
  token: r.token,
});

async function authenticate(path, body) {
  // skipAuth: старий токен не має впливати на вхід/реєстрацію
  const session = toSession(await post(path, body, { skipAuth: true }));
  saveSession(session);
  return session;
}

export const authApi = {
  getSession: loadSession,
  login: (username, password, role) => authenticate("/api/auth/login", { username, password, role }),
  register: (username, password, role) => authenticate("/api/auth/register", { username, password, role }),
  logout: clearSession,
  /** Профіль + бронювання + відгуки + скарги поточного користувача. */
  me: () => request("/api/auth/me"),
};