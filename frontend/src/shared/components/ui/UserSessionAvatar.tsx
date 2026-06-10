import { useId } from "react";

/**
 * Avatar decorativo de sesión con relieve 3D (header).
 * Login real deshabilitado; solo presentación visual.
 */
export function UserSessionAvatar() {
  const uid = useId().replace(/:/g, "");
  const headGrad = `user-head-${uid}`;
  const bodyGrad = `user-body-${uid}`;
  const shineGrad = `user-shine-${uid}`;

  return (
    <div
      className="group relative inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-b from-blue-400 via-blue-500 to-blue-700 p-[2px] shadow-[0_3px_0_0_#1d4ed8,0_5px_14px_rgba(37,99,235,0.45),inset_0_1px_0_rgba(255,255,255,0.35)] transition-transform duration-150 hover:translate-y-px hover:shadow-[0_2px_0_0_#1d4ed8,0_4px_10px_rgba(37,99,235,0.4),inset_0_1px_0_rgba(255,255,255,0.3)] active:translate-y-0.5 active:shadow-[0_1px_0_0_#1d4ed8,0_2px_6px_rgba(37,99,235,0.35)] dark:from-blue-500 dark:via-blue-600 dark:to-blue-900 dark:shadow-[0_3px_0_0_#1e3a8a,0_5px_14px_rgba(30,64,175,0.55),inset_0_1px_0_rgba(255,255,255,0.2)] dark:hover:shadow-[0_2px_0_0_#1e3a8a,0_4px_10px_rgba(30,64,175,0.5),inset_0_1px_0_rgba(255,255,255,0.15)]"
      aria-hidden="true"
    >
      <span className="flex h-full w-full items-center justify-center rounded-full bg-gradient-to-b from-white/25 to-transparent">
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
            <linearGradient
              id={headGrad}
              x1="12"
              y1="3"
              x2="12"
              y2="11"
              gradientUnits="userSpaceOnUse"
            >
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
            <linearGradient
              id={shineGrad}
              x1="8"
              y1="4"
              x2="16"
              y2="10"
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0%" stopColor="#FFFFFF" stopOpacity="0.85" />
              <stop offset="100%" stopColor="#FFFFFF" stopOpacity="0" />
            </linearGradient>
            <filter id={`user-shadow-${uid}`} x="-20%" y="-20%" width="140%" height="140%">
              <feDropShadow
                dx="0"
                dy="1"
                stdDeviation="0.6"
                floodColor="#0f172a"
                floodOpacity="0.35"
              />
            </filter>
          </defs>
          <g filter={`url(#user-shadow-${uid})`}>
            <circle cx="12" cy="8" r="4" fill={`url(#${headGrad})`} />
            <ellipse
              cx="10.5"
              cy="6.8"
              rx="1.6"
              ry="1.1"
              fill={`url(#${shineGrad})`}
              opacity="0.9"
            />
            <path
              d="M5 20.5c0-3.5 3.13-6 7-6s7 2.5 7 6"
              fill={`url(#${bodyGrad})`}
              stroke="#1E3A8A"
              strokeWidth="0.5"
              strokeLinejoin="round"
            />
          </g>
        </svg>
      </span>
    </div>
  );
}
