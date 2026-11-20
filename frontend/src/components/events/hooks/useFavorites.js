import { useState } from "react";
import { FAVORITES_KEY } from "./constants.js";
import { loadLocalStorageSet, persistLocalStorageSet } from "./storage.js";

export default function useFavorites() {
  const [favoriteIds, setFavoriteIds] = useState(() => loadLocalStorageSet(FAVORITES_KEY));

  function toggleFavorite(eventId, e) {
    e.stopPropagation();
    const next = new Set(favoriteIds);
    if (next.has(eventId)) {
      next.delete(eventId);
    } else {
      next.add(eventId);
    }
    setFavoriteIds(next);
    persistLocalStorageSet(FAVORITES_KEY, next);
  }

  return { favoriteIds, toggleFavorite };
}