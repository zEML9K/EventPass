import EventCard from "./EventCard.jsx";

export default function EventList({ events, favoriteIds, activeId, onToggleFavorite, onSelect }) {
  return (
    <div className="list">
      {events.map((ev) => (
        <EventCard
          key={ev.id}
          event={ev}
          isFavorite={favoriteIds.has(ev.id)}
          active={activeId === ev.id}
          onToggleFavorite={onToggleFavorite}
          onSelect={onSelect}
        />
      ))}
    </div>
  );
}