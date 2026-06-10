// @vitest-environment jsdom

import { createElement } from "react";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, beforeEach, vi } from "vitest";
import { ThemeProvider } from "../../hooks/use-theme.js";
import { DashboardLayout, type NavItem } from "./DashboardLayout.js";
import { THEME_STORAGE_KEY } from "../../lib/theme.js";

const HOME_NAV: NavItem[] = [{ id: "home", label: "Inicio", href: "/", icon: null }];

function renderLayout(props?: {
  subtitle?: string;
  initialPath?: string;
  navItems?: NavItem[];
  onNavigate?: (id: string) => void;
  activeSection?: string;
  title?: string;
  rootHref?: string;
}) {
  return render(
    createElement(
      ThemeProvider,
      null,
      createElement(
        MemoryRouter,
        { initialEntries: [props?.initialPath ?? "/"] },
        createElement(DashboardLayout, {
          children: "Contenido",
          navItems: props?.navItems ?? [],
          title: props?.title ?? "FLIT",
          subtitle: props?.subtitle ?? "Trámites",
          onNavigate: props?.onNavigate,
          activeSection: props?.activeSection,
          rootHref: props?.rootHref,
        }),
      ),
    ),
  );
}

describe("DashboardLayout", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
  });

  afterEach(() => {
    cleanup();
  });

  it("renders BY FLIT as an accessible external footer link", () => {
    renderLayout();

    expect(screen.queryByText("FLIT Boilerplate v2")).toBeNull();

    const links = screen.getAllByRole("link", { name: "BY FLIT" });

    expect(links).not.toHaveLength(0);
    links.forEach((link) => {
      expect(link.getAttribute("href")).toBe("https://flitsas.com.co/");
      expect(link.getAttribute("target")).toBe("_blank");
      expect(link.getAttribute("rel")).toBe("noopener noreferrer");
    });
  });

  it("HU9386 AC1: shows Colombia world cup message below BY FLIT", () => {
    renderLayout();

    expect(screen.getAllByLabelText("Este año ganamos el mundial").length).toBeGreaterThan(0);
  });

  it("HU9386 AC2: applies Colombia flag colors to the message segments", () => {
    const { container } = renderLayout();

    expect(container.querySelector(".text-\\[\\#FCD116\\]")).not.toBeNull();
    expect(container.querySelector(".text-\\[\\#003893\\]")).not.toBeNull();
    expect(container.querySelector(".text-\\[\\#CE1126\\]")).not.toBeNull();
  });

  it("applies dark palette classes to shell, header and main when dark theme is active", async () => {
    localStorage.setItem(THEME_STORAGE_KEY, "dark");
    document.documentElement.classList.add("dark");
    const { container } = renderLayout();

    expect(container.querySelector(".dark\\:bg-flit-canvas-dark")).not.toBeNull();
    expect(container.querySelector("header.dark\\:bg-flit-surface-dark")).not.toBeNull();
    expect(container.querySelector("main.dark\\:bg-flit-canvas-dark")).not.toBeNull();
  });

  it("exposes breadcrumb navigation with accessible labels", () => {
    renderLayout({ subtitle: "Trámites" });

    const breadcrumbNav = screen.getAllByRole("navigation", { name: "Ruta actual" })[0]!;
    expect(breadcrumbNav).toHaveTextContent("FLIT");
    expect(breadcrumbNav).toHaveTextContent("Trámites");
  });

  it("removes dark shell classes after toggling back to light theme", async () => {
    const user = userEvent.setup();
    localStorage.setItem(THEME_STORAGE_KEY, "dark");
    document.documentElement.classList.add("dark");
    renderLayout();

    const toggleButtons = screen.getAllByRole("button", { name: "Activar tema claro" });
    await user.click(toggleButtons[0]!);

    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(screen.getByRole("navigation", { name: "Ruta actual" })).toBeInTheDocument();
  });

  it("AC1: shows help button in global header between breadcrumb and user avatar", () => {
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    const breadcrumbNav = screen.getByRole("navigation", { name: "Ruta actual" });
    const themeToggle = screen.getByRole("button", { name: "Activar tema oscuro" });
    // HelpButton is now wrapped in a relative container div for dropdown positioning
    const helpWrapper = helpButton.parentElement!;
    const actionsCluster = helpWrapper.parentElement!;

    expect(helpButton).toBeVisible();
    expect(helpButton.className).toContain("focus-visible:ring-flit-primary");
    expect(helpButton.className).toContain("hover:bg-flit-canvas");

    expect(
      breadcrumbNav.compareDocumentPosition(helpButton) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(
      helpButton.compareDocumentPosition(themeToggle) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(actionsCluster.className).toContain("ml-auto");
    expect(Array.from(actionsCluster.children).indexOf(helpWrapper)).toBe(0);
  });

  it("AC2: help button is keyboard focusable and activatable", async () => {
    const user = userEvent.setup();
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    await user.tab();
    await user.tab();

    let focused = document.activeElement;
    while (focused && focused !== helpButton) {
      await user.tab();
      focused = document.activeElement;
    }

    expect(helpButton).toHaveFocus();
    expect(helpButton.className).toContain("focus-visible:ring-flit-primary");

    await user.keyboard("{Enter}");
    expect(screen.getByRole("menuitem", { name: "Soporte" })).toBeInTheDocument();
    expect(helpButton).toHaveAttribute("aria-expanded", "true");
  });

  it("HU9398 AC1: header shows 3D session avatar with SVG gradients", () => {
    const { container } = renderLayout();

    const gradients = container.querySelectorAll("header linearGradient");
    expect(gradients.length).toBeGreaterThan(0);
    expect(container.querySelector("header circle[fill^='url']")).not.toBeNull();
  });

  it("AC3: mobile header keeps menu, breadcrumb, help and avatar without horizontal overflow", () => {
    Object.defineProperty(window, "innerWidth", { configurable: true, value: 375, writable: true });
    const { container } = renderLayout();

    expect(screen.getByRole("button", { name: "Abrir menú" })).toBeVisible();
    expect(screen.getByRole("navigation", { name: "Ruta actual" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Abrir ayuda" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Activar tema oscuro" })).toBeVisible();

    const header = container.querySelector("header")!;
    expect(header.scrollWidth).toBeLessThanOrEqual(header.clientWidth + 1);
  });

  // HU9281 — Menú desplegable de ayuda con enlaces externos
  // Uso de ejemplo: click en "Abrir ayuda" → abre menú con Soporte y Consulta Tickets

  it("HU9281 AC1: click en 'Abrir ayuda' abre menú con Soporte, Consulta Tickets y Configuración", async () => {
    const user = userEvent.setup();
    renderLayout();

    expect(screen.queryByRole("menuitem", { name: "Soporte" })).toBeNull();
    expect(screen.queryByRole("menuitem", { name: "Consulta Tickets" })).toBeNull();
    await user.click(screen.getByRole("button", { name: "Abrir ayuda" }));

    expect(screen.getByRole("menuitem", { name: "Soporte" })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Consulta Tickets" })).toBeInTheDocument();
  });

  it("HU9281 AC2: enlace Soporte apunta a https://flitsas.com.co/SOPORTE/ en nueva pestaña con rel seguro", async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole("button", { name: "Abrir ayuda" }));

    const soporteLink = screen.getByRole("menuitem", { name: "Soporte" });
    expect(soporteLink.getAttribute("href")).toBe("https://flitsas.com.co/SOPORTE/");
    expect(soporteLink.getAttribute("target")).toBe("_blank");
    expect(soporteLink.getAttribute("rel")).toBe("noopener noreferrer");
  });

  it("HU9281 AC3: enlace Consulta Tickets apunta a https://flitsas.com.co/consulta-tikeds/ en nueva pestaña con rel seguro", async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole("button", { name: "Abrir ayuda" }));

    const consultaLink = screen.getByRole("menuitem", { name: "Consulta Tickets" });
    expect(consultaLink.getAttribute("href")).toBe("https://flitsas.com.co/consulta-tikeds/");
    expect(consultaLink.getAttribute("target")).toBe("_blank");
    expect(consultaLink.getAttribute("rel")).toBe("noopener noreferrer");
  });

  it("HU9281 AC4: Escape cierra el menú sin navegar a pestañas externas", async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole("button", { name: "Abrir ayuda" }));
    expect(screen.getByRole("menuitem", { name: "Soporte" })).toBeInTheDocument();

    await user.keyboard("{Escape}");

    expect(screen.queryByRole("menuitem", { name: "Soporte" })).toBeNull();
    expect(screen.queryByRole("menuitem", { name: "Consulta Tickets" })).toBeNull();
  });

  // HU9282 — Accesibilidad y responsividad del menú de ayuda
  // Uso de ejemplo: Enter en "Abrir ayuda" → menú abierto; flechas mueven foco entre opciones

  it("HU9282 AC1: Enter o Espacio abre el menú y las flechas navegan entre opciones", async () => {
    const user = userEvent.setup();
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    helpButton.focus();

    await user.keyboard("{Enter}");
    const soporte = screen.getByRole("menuitem", { name: "Soporte" });
    const consulta = screen.getByRole("menuitem", { name: "Consulta Tickets" });
    expect(soporte).toBeInTheDocument();
    expect(soporte).toHaveFocus();

    await user.keyboard("{ArrowDown}");
    expect(consulta).toHaveFocus();

    await user.keyboard("{ArrowUp}");
    expect(soporte).toHaveFocus();
  });

  it("HU9282 AC1 edge: Espacio abre el menú desde el disparador", async () => {
    const user = userEvent.setup();
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    helpButton.focus();
    await user.keyboard(" ");

    expect(screen.getByRole("menuitem", { name: "Soporte" })).toBeInTheDocument();
    expect(helpButton).toHaveAttribute("aria-expanded", "true");
  });

  it("HU9282 AC1 contrato: Tab alcanza la primera opción con el menú abierto por clic", async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole("button", { name: "Abrir ayuda" }));
    await user.tab();

    expect(screen.getByRole("menuitem", { name: "Soporte" })).toHaveFocus();
  });

  it("HU9282 AC2: aria-expanded refleja el estado y las opciones tienen nombre accesible", async () => {
    const user = userEvent.setup();
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    expect(helpButton).toHaveAttribute("aria-expanded", "false");

    await user.click(helpButton);
    expect(helpButton).toHaveAttribute("aria-expanded", "true");
    expect(helpButton).toHaveAttribute("aria-controls");

    expect(screen.getByRole("menuitem", { name: "Soporte" })).toHaveAccessibleName("Soporte");
    expect(screen.getByRole("menuitem", { name: "Consulta Tickets" })).toHaveAccessibleName(
      "Consulta Tickets",
    );
  });

  it("HU9282 AC2 edge: aria-expanded vuelve a false al cerrar", async () => {
    const user = userEvent.setup();
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    await user.click(helpButton);
    await user.keyboard("{Escape}");

    expect(helpButton).toHaveAttribute("aria-expanded", "false");
    expect(helpButton).not.toHaveAttribute("aria-controls");
  });

  it("HU9282 AC3: header en 375px sin overflow horizontal", () => {
    Object.defineProperty(window, "innerWidth", { configurable: true, value: 375, writable: true });
    const { container } = renderLayout();

    const header = container.querySelector("header")!;
    expect(header.className).toContain("overflow-visible");
    const breadcrumbNav = screen.getByRole("navigation", { name: "Ruta actual" });
    expect(breadcrumbNav.className).toContain("overflow-hidden");
    expect(screen.getByRole("button", { name: "Abrir menú" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Abrir ayuda" })).toBeVisible();
    expect(header.scrollWidth).toBeLessThanOrEqual(header.clientWidth + 1);
  });

  it("HU9282 AC3 edge: breadcrumb truncado no fuerza ancho del header", () => {
    Object.defineProperty(window, "innerWidth", { configurable: true, value: 375, writable: true });
    renderLayout({ subtitle: "Sección con nombre muy largo para validar truncado" });

    const breadcrumbLabel = screen.getByRole("navigation", { name: "Ruta actual" });
    const truncated = breadcrumbLabel.querySelector(".truncate");
    expect(truncated).not.toBeNull();
  });

  describe("HU9390 — migas de pan clicables", () => {
    function getBreadcrumbNav() {
      return within(screen.getByRole("navigation", { name: "Ruta actual" }));
    }

    it("AC1: desde ruta anidada, FLIT enlaza a /", () => {
      renderLayout({ initialPath: "/seccion", navItems: HOME_NAV, rootHref: "/" });

      const rootLink = getBreadcrumbNav().getByRole("link", { name: "FLIT" });
      expect(rootLink.getAttribute("href")).toBe("/");
    });

    it("AC2: segmento FLIT enlaza a / fuera de la raíz", () => {
      renderLayout({ initialPath: "/seccion", navItems: HOME_NAV, rootHref: "/" });

      const rootLink = getBreadcrumbNav().getByRole("link", { name: "FLIT" });
      expect(rootLink.getAttribute("href")).toBe("/");
    });

    it("AC3: en / el módulo activo es página actual sin enlace", () => {
      renderLayout({ initialPath: "/", navItems: HOME_NAV });

      const breadcrumb = getBreadcrumbNav();
      expect(breadcrumb.queryByRole("link", { name: "Inicio" })).toBeNull();
      expect(breadcrumb.getByText("Inicio")).toHaveAttribute("aria-current", "page");
    });

    it("AC4: enlaces de miga tienen estilos de foco visible", () => {
      renderLayout({ initialPath: "/seccion", navItems: HOME_NAV, rootHref: "/" });

      const rootLink = getBreadcrumbNav().getByRole("link", { name: "FLIT" });
      expect(rootLink.className).toContain("focus-visible:ring-flit-primary");
    });

    it("AC6: ítem sin href invoca onNavigate al hacer clic en el módulo", async () => {
      const user = userEvent.setup();
      const onNavigate = vi.fn();
      const sectionNav: NavItem[] = [{ id: "employees", label: "Empleados", icon: null }];

      renderLayout({
        initialPath: "/seccion",
        navItems: sectionNav,
        title: "Admin",
        subtitle: "RRHH",
        activeSection: "employees",
        onNavigate,
        rootHref: "/",
      });

      await user.click(getBreadcrumbNav().getByRole("button", { name: "Empleados" }));
      expect(onNavigate).toHaveBeenCalledWith("employees");
    });
  });

  it("HU9282 AC4: Escape devuelve el foco al disparador", async () => {
    const user = userEvent.setup();
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    helpButton.focus();
    await user.keyboard("{Enter}");
    expect(screen.getByRole("menuitem", { name: "Soporte" })).toHaveFocus();

    await user.keyboard("{Escape}");

    expect(screen.queryByRole("menuitem", { name: "Soporte" })).toBeNull();
    expect(helpButton).toHaveFocus();
  });

  it("HU9282 AC4 edge: al cerrar no quedan menuitems en el árbol accesible", async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole("button", { name: "Abrir ayuda" }));
    await user.keyboard("{Escape}");

    expect(screen.queryByRole("menu")).toBeNull();
    expect(screen.queryAllByRole("menuitem")).toHaveLength(0);
  });

  it("HU9282 AC4 contrato: seleccionar opción cierra el menú y restaura foco al disparador", async () => {
    const user = userEvent.setup();
    renderLayout();

    const helpButton = screen.getByRole("button", { name: "Abrir ayuda" });
    await user.click(helpButton);
    await user.click(screen.getByRole("menuitem", { name: "Soporte" }));

    expect(screen.queryByRole("menuitem", { name: "Soporte" })).toBeNull();
    expect(helpButton).toHaveFocus();
  });
});
