import {
  useState,
  useEffect,
  useRef,
  useId,
  useCallback,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { Link, useLocation } from "react-router-dom";
import { ThemeToggle } from "./ThemeToggle.js";
import { UserSessionAvatar } from "./UserSessionAvatar.js";

export interface NavItem {
  id: string;
  label: string;
  icon: ReactNode;
  href?: string;
}

interface DashboardLayoutProps {
  navItems: NavItem[];
  activeSection?: string;
  onNavigate?: (id: string) => void;
  children: ReactNode;
  title?: string;
  subtitle?: string;
  /** Ruta del segmento raíz de la miga de pan (default: /) */
  rootHref?: string;
}

const BREADCRUMB_LINK_CLASS =
  "rounded-sm text-flit-primary underline-offset-2 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 dark:focus-visible:ring-offset-slate-800";

const BREADCRUMB_ROOT_LINK_CLASS =
  "rounded-sm text-flit-muted hover:text-flit-heading dark:text-slate-400 dark:hover:text-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 dark:focus-visible:ring-offset-slate-800";

const BREADCRUMB_CURRENT_CLASS =
  "truncate font-medium text-flit-heading dark:text-flit-heading-dark";

function isNavItemActive(item: NavItem, pathname: string, activeSection: string): boolean {
  if (item.href) {
    return pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
  }
  return item.id === activeSection;
}

function findBreadcrumbNavItem(
  navItems: NavItem[],
  pathname: string,
  activeSection: string,
): NavItem | undefined {
  return navItems.find((item) => {
    if (item.href) {
      return pathname === item.href || pathname.startsWith(`${item.href}/`);
    }
    return item.id === activeSection;
  });
}

function BreadcrumbSeparator() {
  return (
    <span className="text-flit-border dark:text-slate-600" aria-hidden="true">
      /
    </span>
  );
}

function BreadcrumbBar({
  title,
  rootHref,
  pathname,
  moduleLabel,
  moduleHref,
  onModuleNavigate,
  isModuleCurrent,
}: {
  title: string;
  rootHref: string;
  pathname: string;
  moduleLabel: string;
  moduleHref?: string;
  onModuleNavigate?: () => void;
  isModuleCurrent: boolean;
}) {
  const isRootCurrent = pathname === rootHref;

  return (
    <nav
      className="flex min-w-0 flex-1 items-center gap-2 overflow-hidden text-sm"
      aria-label="Ruta actual"
    >
      {isRootCurrent ? (
        <span
          className={`${BREADCRUMB_CURRENT_CLASS} shrink-0 text-flit-muted dark:text-slate-400`}
        >
          {title}
        </span>
      ) : (
        <Link to={rootHref} className={`${BREADCRUMB_ROOT_LINK_CLASS} shrink-0`}>
          {title}
        </Link>
      )}

      <BreadcrumbSeparator />

      {isModuleCurrent || (!moduleHref && !onModuleNavigate) ? (
        <span className={`${BREADCRUMB_CURRENT_CLASS} min-w-0`} aria-current="page">
          {moduleLabel}
        </span>
      ) : moduleHref ? (
        <Link to={moduleHref} className={`${BREADCRUMB_LINK_CLASS} min-w-0 truncate`}>
          {moduleLabel}
        </Link>
      ) : (
        <button
          type="button"
          onClick={onModuleNavigate}
          className={`${BREADCRUMB_LINK_CLASS} min-w-0 truncate text-left`}
        >
          {moduleLabel}
        </button>
      )}
    </nav>
  );
}

function HamburgerIcon() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      aria-hidden="true"
    >
      <path strokeLinecap="round" d="M3 5h14M3 10h14M3 15h14" />
    </svg>
  );
}

function XIcon() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      aria-hidden="true"
    >
      <path strokeLinecap="round" d="M5 5l10 10M15 5L5 15" />
    </svg>
  );
}

function HelpIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="10" />
      <path d="M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3" />
      <path d="M12 17h.01" />
    </svg>
  );
}

const HELP_MENU_ITEMS = [
  { label: "Soporte", href: "https://flitsas.com.co/SOPORTE/" },
  { label: "Consulta Tickets", href: "https://flitsas.com.co/consulta-tikeds/" },
] as const;

function HelpButton() {
  const [isOpen, setIsOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const itemRefs = useRef<(HTMLAnchorElement | null)[]>([]);
  const menuId = useId();

  const closeMenu = useCallback((returnFocusToTrigger = true) => {
    setIsOpen(false);
    setActiveIndex(-1);
    if (returnFocusToTrigger) {
      triggerRef.current?.focus();
    }
  }, []);

  const openMenu = useCallback((focusFirstItem: boolean) => {
    setIsOpen(true);
    setActiveIndex(focusFirstItem ? 0 : -1);
  }, []);

  useEffect(() => {
    if (!isOpen || activeIndex < 0) return;
    itemRefs.current[activeIndex]?.focus();
  }, [isOpen, activeIndex]);

  useEffect(() => {
    if (!isOpen) return;
    function handleKeyDown(e: globalThis.KeyboardEvent) {
      if (e.key === "Escape") {
        e.preventDefault();
        closeMenu(true);
      }
    }
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, closeMenu]);

  function handleTriggerKeyDown(e: KeyboardEvent<HTMLButtonElement>) {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      if (!isOpen) {
        openMenu(true);
      } else {
        closeMenu(true);
      }
      return;
    }
    if (e.key === "ArrowDown") {
      e.preventDefault();
      if (!isOpen) {
        openMenu(true);
      } else {
        setActiveIndex((index) => (index + 1) % HELP_MENU_ITEMS.length);
      }
    }
    if (e.key === "ArrowUp" && isOpen) {
      e.preventDefault();
      setActiveIndex((index) => (index <= 0 ? HELP_MENU_ITEMS.length - 1 : index - 1));
    }
  }

  function handleMenuKeyDown(e: KeyboardEvent<HTMLDivElement>) {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((index) => (index + 1) % HELP_MENU_ITEMS.length);
      return;
    }
    if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((index) => (index <= 0 ? HELP_MENU_ITEMS.length - 1 : index - 1));
      return;
    }
    if (e.key === "Home") {
      e.preventDefault();
      setActiveIndex(0);
      return;
    }
    if (e.key === "End") {
      e.preventDefault();
      setActiveIndex(HELP_MENU_ITEMS.length - 1);
    }
  }

  return (
    <div className="relative z-50">
      <button
        ref={triggerRef}
        type="button"
        aria-label="Abrir ayuda"
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-controls={isOpen ? menuId : undefined}
        onClick={() => {
          if (isOpen) {
            closeMenu(true);
          } else {
            openMenu(false);
          }
        }}
        onKeyDown={handleTriggerKeyDown}
        className="inline-flex shrink-0 items-center justify-center w-9 h-9 rounded-lg text-flit-muted hover:text-flit-heading hover:bg-flit-canvas dark:text-slate-300 dark:hover:text-slate-100 dark:hover:bg-slate-700 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 dark:focus-visible:ring-offset-slate-800"
      >
        <HelpIcon />
      </button>

      {isOpen ? (
        <div
          id={menuId}
          role="menu"
          aria-label="Opciones de ayuda"
          onKeyDown={handleMenuKeyDown}
          className="absolute right-0 top-full z-[100] mt-1 w-48 max-w-[calc(100vw-2rem)] rounded-lg border border-flit-border bg-white shadow-flit-md dark:border-flit-border-dark dark:bg-flit-surface-dark"
        >
          {HELP_MENU_ITEMS.map((item, index) => (
            <a
              key={item.label}
              ref={(el) => {
                itemRefs.current[index] = el;
              }}
              role="menuitem"
              href={item.href}
              target="_blank"
              rel="noopener noreferrer"
              tabIndex={activeIndex < 0 ? (index === 0 ? 0 : -1) : activeIndex === index ? 0 : -1}
              onClick={() => closeMenu(true)}
              className={`flex items-center px-4 py-2.5 text-sm text-flit-body transition-colors hover:bg-flit-canvas focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-flit-primary dark:text-slate-200 dark:hover:bg-slate-700 ${
                index === 0 ? "rounded-t-lg" : ""
              } ${index === HELP_MENU_ITEMS.length - 1 ? "rounded-b-lg" : ""}`}
            >
              {item.label}
            </a>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function BrandLogo({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <div className="flex items-center gap-3 px-6 py-5">
      <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-white/20 backdrop-blur shrink-0">
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="white"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M5 17H3a2 2 0 01-2-2V5a2 2 0 012-2h11l5 5v2" />
          <path d="M14 17h6l-3-3 3-3h-6" />
        </svg>
      </div>
      <div>
        <p className="text-white font-semibold text-sm leading-none">{title}</p>
        <p className="text-white/70 text-xs mt-0.5">{subtitle}</p>
      </div>
    </div>
  );
}

function NavButton({
  item,
  isActive,
  onClick,
}: {
  item: NavItem;
  isActive: boolean;
  onClick: () => void;
}) {
  const className = `w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150 ${
    isActive
      ? "bg-white/20 text-white border border-white/30"
      : "text-white/75 hover:bg-white/10 hover:text-white border border-transparent"
  }`;

  const content = (
    <>
      <span className={`shrink-0 ${isActive ? "text-white" : "text-white/60"}`}>{item.icon}</span>
      {item.label}
      {isActive ? <span className="ml-auto w-1.5 h-1.5 rounded-full bg-flit-accent" /> : null}
    </>
  );

  if (item.href) {
    return (
      <Link
        to={item.href}
        onClick={onClick}
        aria-current={isActive ? "page" : undefined}
        className={className}
      >
        {content}
      </Link>
    );
  }

  return (
    <button
      type="button"
      onClick={onClick}
      aria-current={isActive ? "page" : undefined}
      className={className}
    >
      {content}
    </button>
  );
}

function SidebarNav({
  navItems,
  activeSection,
  onNavigate,
  title,
  subtitle,
}: {
  navItems: NavItem[];
  activeSection: string;
  onNavigate: (id: string) => void;
  title: string;
  subtitle?: string;
}) {
  const location = useLocation();

  return (
    <div className="flex flex-col h-full">
      <BrandLogo title={title} subtitle={subtitle ?? "Trámites"} />

      <div className="mx-3 h-px bg-white/20 mb-2" />

      <nav className="flex-1 px-3 py-2 space-y-1" aria-label="Navegación principal">
        <p className="px-3 mb-2 text-xs font-semibold uppercase tracking-widest text-white/50">
          Módulos
        </p>
        {navItems.map((item) => {
          const isActive = isNavItemActive(item, location.pathname, activeSection);
          return (
            <NavButton
              key={item.id}
              item={item}
              isActive={isActive}
              onClick={() => onNavigate(item.id)}
            />
          );
        })}
      </nav>

      <div className="mx-3 h-px bg-white/20" />
      <div className="px-5 py-4 space-y-1">
        <a
          href="https://flitsas.com.co/"
          target="_blank"
          rel="noopener noreferrer"
          className="text-xs text-white/50 transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/70 focus-visible:ring-offset-2 focus-visible:ring-offset-flit-primary"
        >
          BY FLIT
        </a>
        <p className="text-xs font-semibold leading-snug" aria-label="Este año ganamos el mundial">
          <span className="text-[#FCD116]">Este año </span>
          <span className="text-[#003893]">ganamos </span>
          <span className="text-[#CE1126]">el mundial</span>
        </p>
      </div>
    </div>
  );
}

export function DashboardLayout({
  navItems,
  activeSection = "",
  onNavigate,
  children,
  title = "FLIT",
  subtitle,
  rootHref = "/",
}: DashboardLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();

  const activeItem = findBreadcrumbNavItem(navItems, location.pathname, activeSection);

  function handleNavigate(id: string) {
    onNavigate?.(id);
    setSidebarOpen(false);
  }

  const moduleLabel = activeItem?.label ?? subtitle ?? "Inicio";
  const moduleHref = activeItem?.href;
  const isModuleCurrent = moduleHref ? location.pathname === moduleHref : false;

  return (
    <div className="flex h-screen bg-flit-canvas dark:bg-flit-canvas-dark overflow-hidden">
      {sidebarOpen ? (
        <div
          className="fixed inset-0 bg-black/60 dark:bg-black/75 z-20 lg:hidden backdrop-blur-sm"
          aria-hidden="true"
          onClick={() => setSidebarOpen(false)}
        />
      ) : null}

      <aside className="hidden lg:flex lg:flex-col w-60 flit-sidebar-gradient shrink-0 shadow-flit-md">
        <SidebarNav
          navItems={navItems}
          activeSection={activeSection}
          onNavigate={handleNavigate}
          title={title}
          subtitle={subtitle}
        />
      </aside>

      <aside
        className={`fixed inset-y-0 left-0 z-30 w-60 flit-sidebar-gradient flex flex-col transform transition-transform duration-300 ease-in-out lg:hidden ${
          sidebarOpen ? "translate-x-0" : "-translate-x-full"
        }`}
        aria-label="Menú de navegación"
      >
        <div className="flex justify-end pt-3 pr-3">
          <button
            onClick={() => setSidebarOpen(false)}
            aria-label="Cerrar menú"
            className="text-white/80 hover:text-white p-1.5 rounded-lg hover:bg-white/10 transition-colors"
            type="button"
          >
            <XIcon />
          </button>
        </div>
        <SidebarNav
          navItems={navItems}
          activeSection={activeSection}
          onNavigate={handleNavigate}
          title={title}
          subtitle={subtitle}
        />
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden">
        <header className="relative z-40 overflow-visible bg-white dark:bg-flit-surface-dark border-b border-flit-border dark:border-flit-border-dark px-4 sm:px-6 h-14 flex items-center gap-2 sm:gap-3 shrink-0 shadow-flit dark:shadow-none">
          <button
            onClick={() => setSidebarOpen(true)}
            aria-label="Abrir menú"
            className="lg:hidden text-flit-muted hover:text-flit-heading dark:text-flit-muted-dark dark:hover:text-flit-heading-dark p-1.5 rounded-lg hover:bg-flit-canvas dark:hover:bg-slate-700 transition-colors"
            type="button"
          >
            <HamburgerIcon />
          </button>

          <BreadcrumbBar
            title={title}
            rootHref={rootHref}
            pathname={location.pathname}
            moduleLabel={moduleLabel}
            moduleHref={moduleHref}
            onModuleNavigate={
              activeItem && !moduleHref && onNavigate
                ? () => handleNavigate(activeItem.id)
                : undefined
            }
            isModuleCurrent={isModuleCurrent}
          />

          <div className="relative z-50 ml-auto flex shrink-0 items-center gap-2 sm:gap-3 overflow-visible">
            <HelpButton />
            <ThemeToggle />
            <UserSessionAvatar />
          </div>
        </header>

        <main
          className="flex-1 overflow-y-auto bg-flit-canvas dark:bg-flit-canvas-dark"
          id="main-content"
        >
          {children}
        </main>
      </div>
    </div>
  );
}
