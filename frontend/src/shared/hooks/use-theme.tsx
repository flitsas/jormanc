import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  applyThemeClass,
  persistTheme,
  readStoredTheme,
  resolveIsDark,
  type ThemePreference,
} from "../lib/theme.js";

interface ThemeContextValue {
  preference: ThemePreference;
  isDark: boolean;
  setPreference: (preference: ThemePreference) => void;
  toggleTheme: () => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>(() => readStoredTheme());
  const [isDark, setIsDark] = useState(() => resolveIsDark(readStoredTheme()));

  const setPreference = useCallback((next: ThemePreference) => {
    setPreferenceState(next);
    persistTheme(next);
    const dark = resolveIsDark(next);
    setIsDark(dark);
    applyThemeClass(dark);
  }, []);

  const toggleTheme = useCallback(() => {
    setPreference(isDark ? "light" : "dark");
  }, [isDark, setPreference]);

  useEffect(() => {
    const dark = resolveIsDark(preference);
    setIsDark(dark);
    applyThemeClass(dark);
  }, [preference]);

  useEffect(() => {
    if (preference !== "system" || typeof window.matchMedia !== "function") return;
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = () => {
      const dark = media.matches;
      setIsDark(dark);
      applyThemeClass(dark);
    };
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, [preference]);

  const value = useMemo(
    () => ({ preference, isDark, setPreference, toggleTheme }),
    [preference, isDark, setPreference, toggleTheme],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) {
    throw new Error("useTheme must be used within ThemeProvider");
  }
  return ctx;
}
