import { useState, useEffect, useRef, useId, useCallback, type KeyboardEvent } from "react";
import { useNavigate } from "react-router-dom";
import { getStoredUser, useLogout } from "../../../features/auth/api/auth.api.js";

function UserAvatarIcon() {
  const uid = useId().replace(/:/g, "");
  const headGrad = `user-head-${uid}`;
  const bodyGrad = `user-body-${uid}`;
  const shineGrad = `user-shine-${uid}`;

  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className="drop-shadow-[0_1px_2px_rgba(15,23,42,0.35)]"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id={headGrad} x1="12" y1="3" x2="12" y2="11" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#93C5FD" />
          <stop offset="55%" stopColor="#3B82F6" />
          <stop offset="100%" stopColor="#1D4ED8" />
        </linearGradient>
        <linearGradient
          id={bodyGrad}
          x1="12"
          y1="13"
          x2="12"
          y2="22"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0%" stopColor="#60A5FA" />
          <stop offset="100%" stopColor="#1E40AF" />
        </linearGradient>
        <linearGradient id={shineGrad} x1="8" y1="4" x2="16" y2="10" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#FFFFFF" stopOpacity="0.85" />
          <stop offset="100%" stopColor="#FFFFFF" stopOpacity="0" />
        </linearGradient>
        <filter id={`user-shadow-${uid}`} x="-20%" y="-20%" width="140%" height="140%">
          <feDropShadow dx="0" dy="1" stdDeviation="0.6" floodColor="#0f172a" floodOpacity="0.35" />
        </filter>
      </defs>
      <g filter={`url(#user-shadow-${uid})`}>
        <circle cx="12" cy="8" r="4" fill={`url(#${headGrad})`} />
        <ellipse cx="10.5" cy="6.8" rx="1.6" ry="1.1" fill={`url(#${shineGrad})`} opacity="0.9" />
        <path
          d="M5 20.5c0-3.5 3.13-6 7-6s7 2.5 7 6"
          fill={`url(#${bodyGrad})`}
          stroke="#1E3A8A"
          strokeWidth="0.5"
          strokeLinejoin="round"
        />
      </g>
    </svg>
  );
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0] ?? ""}${parts[parts.length - 1][0] ?? ""}`.toUpperCase();
}

/**
 * Menú de sesión del usuario autenticado (avatar + cerrar sesión).
 */
export function UserSessionAvatar() {
  const user = getStoredUser();
  const logout = useLogout();
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const logoutRef = useRef<HTMLButtonElement>(null);
  const menuId = useId();

  const closeMenu = useCallback((returnFocusToTrigger = true) => {
    setIsOpen(false);
    if (returnFocusToTrigger) {
      triggerRef.current?.focus();
    }
  }, []);

  useEffect(() => {
    if (!isOpen) return;
    logoutRef.current?.focus();
  }, [isOpen]);

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

  async function handleLogout() {
    setIsLoggingOut(true);
    try {
      await logout();
    } finally {
      setIsLoggingOut(false);
      closeMenu(false);
    }
  }

  function handleTriggerKeyDown(e: KeyboardEvent<HTMLButtonElement>) {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      setIsOpen((open) => !open);
      return;
    }
    if (e.key === "ArrowDown" && !isOpen) {
      e.preventDefault();
      setIsOpen(true);
    }
  }

  if (!user) {
    return (
      <button
        type="button"
        onClick={() => navigate("/login")}
        className="inline-flex shrink-0 items-center justify-center rounded-lg px-3 py-1.5 text-sm font-medium text-flit-primary hover:bg-flit-canvas dark:hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary"
      >
        Iniciar sesión
      </button>
    );
  }

  const displayName = user.name || user.email;
  const initials = getInitials(displayName);

  return (
    <div className="relative z-50">
      <button
        ref={triggerRef}
        type="button"
        aria-label={`Menú de usuario: ${displayName}`}
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-controls={isOpen ? menuId : undefined}
        onClick={() => setIsOpen((open) => !open)}
        onKeyDown={handleTriggerKeyDown}
        className="group relative inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-b from-blue-400 via-blue-500 to-blue-700 p-[2px] shadow-[0_3px_0_0_#1d4ed8,0_5px_14px_rgba(37,99,235,0.45),inset_0_1px_0_rgba(255,255,255,0.35)] transition-transform duration-150 hover:translate-y-px focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 dark:focus-visible:ring-offset-slate-800 dark:from-blue-500 dark:via-blue-600 dark:to-blue-900"
      >
        <span className="flex h-full w-full items-center justify-center rounded-full bg-gradient-to-b from-white/25 to-transparent text-[10px] font-bold text-white">
          {initials.length <= 2 ? initials : <UserAvatarIcon />}
        </span>
      </button>

      {isOpen ? (
        <div
          id={menuId}
          ref={menuRef}
          role="menu"
          aria-label="Opciones de sesión"
          className="absolute right-0 top-full z-[100] mt-1 w-56 max-w-[calc(100vw-2rem)] rounded-lg border border-flit-border bg-white shadow-flit-md dark:border-flit-border-dark dark:bg-flit-surface-dark"
        >
          <div className="border-b border-flit-border px-4 py-3 dark:border-flit-border-dark">
            <p className="truncate text-sm font-semibold text-flit-heading dark:text-flit-heading-dark">
              {displayName}
            </p>
            <p className="truncate text-xs text-flit-muted dark:text-slate-400">{user.email}</p>
            {user.tenantName ? (
              <p className="mt-1 truncate text-xs text-flit-muted dark:text-slate-500">
                {user.tenantName}
              </p>
            ) : null}
          </div>
          <button
            ref={logoutRef}
            type="button"
            role="menuitem"
            disabled={isLoggingOut}
            onClick={() => void handleLogout()}
            className="flex w-full items-center gap-2 rounded-b-lg px-4 py-2.5 text-left text-sm text-red-600 transition-colors hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-flit-primary disabled:opacity-60 dark:text-red-400 dark:hover:bg-red-950/40"
          >
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
              <polyline points="16 17 21 12 16 7" />
              <line x1="21" y1="12" x2="9" y2="12" />
            </svg>
            {isLoggingOut ? "Cerrando sesión…" : "Cerrar sesión"}
          </button>
        </div>
      ) : null}
    </div>
  );
}
