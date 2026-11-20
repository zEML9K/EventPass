import { scopedKey } from "../../../auth/session.js";

export function loadLocalStorageSet(key) {
  try {
    return new Set(JSON.parse(localStorage.getItem(scopedKey(key)) || "[]"));
  } catch {
    return new Set();
  }
}

export function persistLocalStorageSet(key, set) {
  localStorage.setItem(scopedKey(key), JSON.stringify([...set]));
}

export function saveLocalStorageSet(key, eventId) {
  const next = loadLocalStorageSet(key);
  next.add(eventId);
  persistLocalStorageSet(key, next);
  return next;
}