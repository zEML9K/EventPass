import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { authApi } from "./authApi.js";
import { UNAUTHORIZED_EVENT } from "./session.js";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => authApi.getSession());
  const [modalOpen, setModalOpen] = useState(false);
  const [notice, setNotice] = useState("");

  // 401 з будь-якого запиту: сесія вже очищена в api.js
  useEffect(() => {
    const onUnauthorized = () => {
      setSession(null);
      setNotice("Сесію завершено. Увійдіть знову.");
      setModalOpen(true);
    };
    window.addEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
  }, []);

  const value = useMemo(() => {
    const finish = (s) => {
      setSession(s);
      setNotice("");
      setModalOpen(false);
    };
    return {
      session,
      role: session?.role ?? null,
      modalOpen,
      notice,
      openLogin: () => setModalOpen(true),
      closeLogin: () => {
        setModalOpen(false);
        setNotice("");
      },
      login: async (u, p, r) => finish(await authApi.login(u, p, r)),
      register: async (u, p, r) => finish(await authApi.register(u, p, r)),
      logout: () => {
        authApi.logout();
        setSession(null);
      },
    };
  }, [session, modalOpen, notice]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);