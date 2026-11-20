import { useState } from "react";
import { AuthProvider, useAuth } from "./auth/AuthContext.jsx";
import { ROLE_LABELS } from "./auth/roles.js";
import EventsView from "./components/EventsView.jsx";
import CreateEventView from "./components/CreateEventView.jsx";
import ScanView from "./components/ScanView.jsx";
import BookingsView from "./components/BookingsView.jsx";
import ActivityView from "./components/ActivityView.jsx";
import AuthModal from "./components/AuthModal.jsx";

// roles: null — доступно всім (включно з гостем); [] — будь-який автентифікований
const TABS = [
  { id: "events", label: "Події", role: "Усі", roles: null },
  { id: "favorites", label: "Обране", role: "Закладки", roles: null },
  { id: "bookings", label: "Мої бронювання", role: "Відвідувач", roles: ["Visitor"] },
  { id: "account", label: "Мій акаунт", role: "Активність", roles: ["Visitor", "Organizer", "Controller"] },
  { id: "create", label: "Створити подію", role: "Організатор", roles: ["Organizer"] },
  { id: "scan", label: "Перевірка квитків", role: "Контролер", roles: ["Controller"] },
];

function AppShell() {
  const { session, role, openLogin, logout } = useAuth();
  const [tab, setTab] = useState("events");
  // Лічильник змушує список подій перезавантажитись після створення нової
  const [version, setVersion] = useState(0);

  const visibleTabs = TABS.filter((t) => !t.roles || (role && t.roles.includes(role)));
  // Якщо вкладка стала недоступною (вихід, інша роль) — повертаємось до подій
  const activeTab = visibleTabs.some((t) => t.id === tab) ? tab : "events";

  return (
    <div className="app">
      <header className="header">
        <h1>
          Event<span>Pass</span>
        </h1>
        <nav className="tabs">
          {visibleTabs.map((t) => (
            <button
              key={t.id}
              className={activeTab === t.id ? "tab active" : "tab"}
              onClick={() => setTab(t.id)}
            >
              {t.label}
              <small>{t.role}</small>
            </button>
          ))}
        </nav>

        <div className="auth-box">
          {session ? (
            <>
              <span className="badge role">{ROLE_LABELS[role] ?? role}</span>
              <span className="muted">{session.username}</span>
              <button className="secondary" style={{ marginTop: 0 }} onClick={logout}>
                Вийти
              </button>
            </>
          ) : (
            <button onClick={openLogin}>Увійти</button>
          )}
        </div>
      </header>

      <main>
        {(activeTab === "events" || activeTab === "favorites") && (
          <EventsView
            key={`${version}-${activeTab}-${session?.userId ?? "guest"}`}
            initialFavoriteFilter={activeTab === "favorites"}
          />
        )}
        {activeTab === "bookings" && <BookingsView />}
        {activeTab === "account" && <ActivityView />}
        {activeTab === "create" && (
          <CreateEventView
            onCreated={() => {
              setVersion((v) => v + 1);
              setTab("events");
            }}
          />
        )}
        {activeTab === "scan" && <ScanView />}
      </main>

      <AuthModal />
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <AppShell />
    </AuthProvider>
  );
}