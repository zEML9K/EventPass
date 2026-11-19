import { CATEGORIES } from "./hooks/constants.js";

export default function EventFilters({ query, onQueryChange, category, onCategoryChange }) {
  return (
    <>
      <input
        className="search"
        type="search"
        placeholder="Пошук події за назвою…"
        value={query}
        onChange={(e) => onQueryChange(e.target.value)}
      />

      <select
        className="search"
        aria-label="Фільтр за категорією"
        value={category}
        onChange={(e) => onCategoryChange(e.target.value)}
      >
        <option value="">Усі категорії</option>
        {CATEGORIES.map((cat) => (
          <option key={cat} value={cat}>
            {cat}
          </option>
        ))}
      </select>
    </>
  );
}