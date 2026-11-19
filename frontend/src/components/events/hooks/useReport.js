import { useEffect, useState } from "react";
import { api } from "../../../api.js";
import { REPORT_CATEGORIES, REPORTED_KEY } from "./constants.js";
import { loadLocalStorageSet, saveLocalStorageSet } from "./storage.js";

export default function useReport({ activeDetailId, selected }) {
  const [category, setCategory] = useState(REPORT_CATEGORIES[0]);
  const [comment, setComment] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [reportedIds, setReportedIds] = useState(() => loadLocalStorageSet(REPORTED_KEY));

  useEffect(() => {
    setSuccess(false);
    if (!activeDetailId) return;
    setError("");
  }, [activeDetailId]);

  async function submit(e) {
    e.preventDefault();
    if (!selected) return;
    setBusy(true);
    setError("");
    try {
      await api.reportEvent(selected.id, category, comment);
      setSuccess(true);
      setComment("");
      setReportedIds(saveLocalStorageSet(REPORTED_KEY, selected.id));
    } catch (err) {
      if (err.code === "already_reported" || err.status === 409) {
        setReportedIds(saveLocalStorageSet(REPORTED_KEY, selected.id));
        setSuccess(true);
      } else {
        setError(err.message);
      }
    } finally {
      setBusy(false);
    }
  }

  return { category, setCategory, comment, setComment, busy, error, success, reportedIds, submit };
}