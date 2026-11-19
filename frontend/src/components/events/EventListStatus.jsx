export default function EventListStatus({ loading, error, isEmpty, favoritesMode, query, category }) {
  return (
    <>
      {loading && <p className="muted">Завантаження…</p>}
      {error && <p className="alert error">{error}</p>}
      {!loading && !error && isEmpty && (
        <p className="muted">
          {favoritesMode
            ? "У вашому обраному поки немає подій."
            : query.trim()
            ? `За запитом «${query}» нічого не знайдено.`
            : category
            ? `Подій у категорії «${category}» не знайдено.`
            : "Подій поки немає."}
        </p>
      )}
    </>
  );
}