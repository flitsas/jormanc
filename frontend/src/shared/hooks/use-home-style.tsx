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
  HOME_STYLE_PRESETS,
  HOME_STYLE_STORAGE_KEY,
  applyHomeStyle,
  initHomeStyleFromStorage,
  persistHomeStyle,
  resolveHomeStyleId,
  type HomeStyleId,
  type HomeStylePreset,
} from "../lib/home-style.js";
import { useTheme } from "./use-theme.js";

interface HomeStyleContextValue {
  styleId: HomeStyleId;
  setStyleId: (id: HomeStyleId) => void;
  presets: readonly HomeStylePreset[];
}

const HomeStyleContext = createContext<HomeStyleContextValue | null>(null);

function HomeStyleThemeSync() {
  const { styleId } = useHomeStyle();
  const { isDark, preference } = useTheme();

  useEffect(() => {
    applyHomeStyle(styleId);
  }, [styleId, isDark, preference]);

  return null;
}

export function HomeStyleProvider({ children }: { children: ReactNode }) {
  const [styleId, setStyleIdState] = useState<HomeStyleId>(() => initHomeStyleFromStorage());

  const setStyleId = useCallback((id: HomeStyleId) => {
    setStyleIdState(id);
    persistHomeStyle(id);
    applyHomeStyle(id);
  }, []);

  useEffect(() => {
    applyHomeStyle(styleId);
  }, [styleId]);

  useEffect(() => {
    function handleStorage(event: StorageEvent) {
      if (event.key !== HOME_STYLE_STORAGE_KEY || event.newValue == null) return;
      const resolved = resolveHomeStyleId(event.newValue);
      setStyleIdState(resolved);
      applyHomeStyle(resolved);
    }
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);

  const value = useMemo(
    () => ({ styleId, setStyleId, presets: HOME_STYLE_PRESETS }),
    [styleId, setStyleId],
  );

  return (
    <HomeStyleContext.Provider value={value}>
      <HomeStyleThemeSync />
      {children}
    </HomeStyleContext.Provider>
  );
}

export function useHomeStyle(): HomeStyleContextValue {
  const ctx = useContext(HomeStyleContext);
  if (!ctx) {
    throw new Error("useHomeStyle must be used within HomeStyleProvider");
  }
  return ctx;
}
