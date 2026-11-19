import { useEffect, useState } from "react";
import { api } from "../../../api.js";
import { RATED_KEY } from "./constants.js";
import { loadLocalStorageSet, saveLocalStorageSet } from "./storage.js";

export default function useRating({ activeDetailId, selected, updateEvent }) {
  const [reviews, setReviews] = useState([]);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [ratedIds, setRatedIds] = useState(() => loadLocalStorageSet(RATED_KEY));

  useEffect(() => {
    if (!activeDetailId) {
      setReviews([]);
      return;
    }
    setError("");
    api
      .getEventReviews(activeDetailId)
      .then(setReviews)
      .catch(() => setReviews([]));
  }, [activeDetailId]);

  async function submit(e) {
    e.preventDefault();
    if (!selected) return;
    setBusy(true);
    setError("");
    try {
      const updated = await api.rateEvent(selected.id, Number(rating), comment);
      updateEvent(updated);
      setComment("");
      setRatedIds(saveLocalStorageSet(RATED_KEY, selected.id));
      const freshReviews = await api.getEventReviews(selected.id);
      setReviews(freshReviews);
    } catch (err) {
      if (err.code === "already_rated" || err.status === 409) {
        setRatedIds(saveLocalStorageSet(RATED_KEY, selected.id));
      } else {
        setError(err.message);
      }
    } finally {
      setBusy(false);
    }
  }

  return { reviews, rating, setRating, comment, setComment, busy, error, ratedIds, submit };
}