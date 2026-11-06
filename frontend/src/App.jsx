import { useState } from "react";
import EventsView from "./components/EventsView.jsx";
import CreateEventView from "./components/CreateEventView.jsx";
import ScanView from "./components/ScanView.jsx";
import BookingsView from "./components/BookingsView.jsx";

const TABS = [
  { id: "events", label: "Події", role: "Відвідувач" },
  { id: "bookings", label: "Мої бронювання", role: "Відвідувач" },
  { id: "create", label: "Створити подію", role: "Організатор" },
  { id: "scan", label: "Перевірка квитків", role: "Контролер" },
];

export default function App() {
  const [tab, setTab] = useState("events");
  // Лічильник змушує список подій перезавантажитись після створення нової
  const [version, setVersion] = useState(0);

  return (
    <div className="app">
      <header className="header">
        <h1>
          Event<span>Pass</span>
        </h1>
        <nav className="tabs">
          {TABS.map((t) => (
            <button
              key={t.id}
              className={tab === t.id ? "tab active" : "tab"}
              onClick={() => setTab(t.id)}
            >
              {t.label}
              <small>{t.role}</small>
            </button>
          ))}
        </nav>
      </header>

      <main>
        {tab === "events" && <EventsView key={version} />}
        {tab === "bookings" && <BookingsView />}
        {tab === "create" && (
          <CreateEventView
            onCreated={() => {
              setVersion((v) => v + 1);
              setTab("events");
            }}
          />
        )}
        {tab === "scan" && <ScanView />}
      </main>
    </div>
  );
}
