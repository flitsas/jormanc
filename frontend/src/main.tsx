import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { PrimeReactProvider, addLocale } from "primereact/api";
import "primereact/resources/primereact.min.css";
import "primeicons/primeicons.css";
import "./styles/prime-flit.css";
import "./index.css";
import { PRIME_LOCALE_ES } from "./shared/lib/prime-setup.js";
import { syncPrimeThemeFromDocument } from "./shared/lib/prime-theme.js";
import { App } from "./App.js";

addLocale("es", PRIME_LOCALE_ES);
syncPrimeThemeFromDocument();

const root = document.getElementById("root");
if (!root) throw new Error("No #root element found");
createRoot(root).render(
  <StrictMode>
    <PrimeReactProvider value={{ locale: "es" }}>
      <App />
    </PrimeReactProvider>
  </StrictMode>,
);
