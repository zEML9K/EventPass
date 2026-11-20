import { clearSession, getToken, UNAUTHORIZED_EVENT } from "./auth/session.js";

const BASE = import.meta.env.VITE_API_URL ?? "";

export class ApiError extends Error {
  constructor(message, code, status) {
    super(message);
    this.code = code;
    this.status = status;
  }
}

export async function request(path, options = {}) {
  const { skipAuth, headers, ...rest } = options;
  const token = skipAuth ? null : getToken();

  let res;
  try {
    res = await fetch(`${BASE}${path}`, {
      ...rest,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...headers,
      },
    });
  } catch {
    throw new ApiError("Немає зв'язку з сервером.", "network_error", 0);
  }

  const data = await res.json().catch(() => null);
  if (!res.ok) {
    // Токен був, але сервер його не прийняв — сесія недійсна
    if (res.status === 401 && token) {
      clearSession();
      window.dispatchEvent(new Event(UNAUTHORIZED_EVENT));
    }
    const fallback =
      res.status === 401 ? "Потрібна авторизація."
      : res.status === 403 ? "Недостатньо прав для цієї дії."
      : `Помилка ${res.status}`;
    throw new ApiError(
      data?.message ?? data?.title ?? fallback,
      data?.code ?? (res.status === 403 ? "forbidden_role" : "unknown"),
      res.status
    );
  }
  return data;
}

export const post = (path, body, options = {}) =>
  request(path, { method: "POST", body: JSON.stringify(body), ...options });

export const api = {
  getEvents: (category) => {
    const query = category ? `?category=${encodeURIComponent(category)}` : "";
    return request(`/api/events${query}`);
  },
  getEvent: (id) => request(`/api/events/${id}`),
  getEventReviews: (id) => request(`/api/events/${id}/reviews`),
  // clientId більше не передаємо: сервер бере його з акаунту
  rateEvent: (id, rating, comment) => post(`/api/events/${id}/reviews`, { rating, comment }),
  reportEvent: (id, category, comment) => post(`/api/events/${id}/reports`, { category, comment }),
  getVenues: () => request("/api/venues"),
  getBookings: () => request("/api/bookings"),
  cancelBooking: (id) => post(`/api/bookings/${id}/cancel`, {}),
  createEvent: (body) => post("/api/events", body),
  book: (eventId, quantity) => post("/api/bookings", { eventId, quantity }),
  checkTicket: (token) => post("/api/tickets/check", { token }),
};