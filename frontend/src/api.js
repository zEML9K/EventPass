const BASE = import.meta.env.VITE_API_URL ?? "";

function getClientId() {
  const key = "eventpass_client_id";
  let id = localStorage.getItem(key);
  if (!id) {
    id = (crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random().toString(36).slice(2)}`);
    localStorage.setItem(key, id);
  }
  return id;
}

export class ApiError extends Error {
  constructor(message, code, status) {
    super(message);
    this.code = code;
    this.status = status;
  }
}

async function request(path, options = {}) {
  let res;
  try {
    res = await fetch(`${BASE}${path}`, {
      headers: { "Content-Type": "application/json" },
      ...options,
    });
  } catch {
    throw new ApiError("Немає зв'язку з сервером.", "network_error", 0);
  }

  const data = await res.json().catch(() => null);
  if (!res.ok) {
    throw new ApiError(
      data?.message ?? data?.title ?? `Помилка ${res.status}`,
      data?.code ?? "unknown",
      res.status
    );
  }
  return data;
}

const post = (path, body) =>
  request(path, { method: "POST", body: JSON.stringify(body) });

export const api = {
  getEvents: (category) => {
    const query = category ? `?category=${encodeURIComponent(category)}` : "";
    return request(`/api/events${query}`);
  },
  getEvent: (id) => request(`/api/events/${id}`),
  getEventReviews: (id) => request(`/api/events/${id}/reviews`),
  rateEvent: (id, rating, comment) =>
    post(`/api/events/${id}/reviews`, { rating, comment, clientId: getClientId() }),
  reportEvent: (id, category, comment) =>
    post(`/api/events/${id}/reports`, { category, comment, clientId: getClientId() }),
  getVenues: () => request("/api/venues"),
  getBookings: () => request("/api/bookings"),
  cancelBooking: (id) => post(`/api/bookings/${id}/cancel`, {}),
  createEvent: (body) => post("/api/events", body),
  book: (eventId, quantity) => post("/api/bookings", { eventId, quantity }),
  checkTicket: (token) => post("/api/tickets/check", { token }),
};