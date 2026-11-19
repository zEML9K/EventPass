import { useEffect, useState } from "react";
import { api } from "../../../api.js";

export default function useEvents() {
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true);
    setError("");
    api
      .getEvents("")
      .then((list) => {
        setEvents(list);
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  const updateEvent = (updated) =>
    setEvents((list) => list.map((e) => (e.id === updated.id ? updated : e)));

  return { events, loading, error, updateEvent };
}