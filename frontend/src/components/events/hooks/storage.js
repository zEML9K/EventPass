export function loadLocalStorageSet(key) {
  try {
    return new Set(JSON.parse(localStorage.getItem(key) || "[]"));
  } catch {
    return new Set();
  }
}

export function saveLocalStorageSet(key, eventId) {
  const next = loadLocalStorageSet(key);
  next.add(eventId);
  localStorage.setItem(key, JSON.stringify([...next]));
  return next;
}