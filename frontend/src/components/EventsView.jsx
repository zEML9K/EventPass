import { useMemo, useState } from "react";
import useEvents from "./events/hooks/useEvents.js";
import useFavorites from "./events/hooks/useFavorites.js";
import useRating from "./events/hooks/useRating.js";
import useReport from "./events/hooks/useReport.js";
import EventFilters from "./events/EventFilters.jsx";
import EventListStatus from "./events/EventListStatus.jsx";
import EventList from "./events/EventList.jsx";
import EventDetailContent from "./events/EventDetailContent.jsx";
import EventModal from "./events/EventModal.jsx";

export default function EventsView({ initialFavoriteFilter = false }) {
  const { events, loading, error, updateEvent } = useEvents();
  const { favoriteIds, toggleFavorite } = useFavorites();
  const [selectedId, setSelectedId] = useState(null);
  const [query, setQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("");

  // Modal state for favorites view
  const [modalEventId, setModalEventId] = useState(null);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const cat = selectedCategory.trim().toLowerCase();

    return events.filter((e) => {
      if (initialFavoriteFilter) {
        return favoriteIds.has(e.id);
      }

      const matchesTitle = !q || e.title.toLowerCase().includes(q);
      const matchesCategory = !cat || (e.category ?? "").toLowerCase() === cat;
      return matchesTitle && matchesCategory;
    });
  }, [events, query, selectedCategory, favoriteIds, initialFavoriteFilter]);

  const activeDetailId = initialFavoriteFilter ? modalEventId : selectedId;
  const selected = filtered.find((e) => e.id === activeDetailId);

  const rating = useRating({ activeDetailId, selected, updateEvent });
  const report = useReport({ activeDetailId, selected });

  function handleSelect(id) {
    if (initialFavoriteFilter) {
      setModalEventId(id);
    } else {
      setSelectedId(id === selectedId ? null : id);
    }
  }

  const renderDetailContent = (ev) => (
    <EventDetailContent
      event={ev}
      isFavorite={favoriteIds.has(ev.id)}
      onToggleFavorite={toggleFavorite}
      onUpdated={updateEvent}
      rating={rating}
      report={report}
    />
  );

  return (
    <>
      <div className={!initialFavoriteFilter && selected ? "split" : ""}>
        <section className="list-col">
          {!initialFavoriteFilter && (
            <EventFilters
              query={query}
              onQueryChange={setQuery}
              category={selectedCategory}
              onCategoryChange={setSelectedCategory}
            />
          )}

          <EventListStatus
            loading={loading}
            error={error}
            isEmpty={filtered.length === 0}
            favoritesMode={initialFavoriteFilter}
            query={query}
            category={selectedCategory}
          />

          <EventList
            events={filtered}
            favoriteIds={favoriteIds}
            activeId={activeDetailId}
            onToggleFavorite={toggleFavorite}
            onSelect={handleSelect}
          />
        </section>

        {!initialFavoriteFilter && selected && (
          <aside className="detail">{renderDetailContent(selected)}</aside>
        )}
      </div>

      {initialFavoriteFilter && modalEventId && selected && (
        <EventModal onClose={() => setModalEventId(null)}>{renderDetailContent(selected)}</EventModal>
      )}
    </>
  );
}