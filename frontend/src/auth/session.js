const SESSION_KEY = "eventpass_session";
export const UNAUTHORIZED_EVENT = "eventpass:unauthorized";

export function loadSession() {
  try {
    const s = JSON.parse(localStorage.getItem(SESSION_KEY) || "null");
    return s?.token ? s : null;
  } catch {
    return null;
  }
}

export const saveSession = (s) => localStorage.setItem(SESSION_KEY, JSON.stringify(s));
export const clearSession = () => localStorage.removeItem(SESSION_KEY);
export const getToken = () => loadSession()?.token ?? null;

/** Ключ localStorage, прив'язаний до поточного користувача (або гостя). */
export const scopedKey = (base) => `${base}:${loadSession()?.userId ?? "guest"}`;