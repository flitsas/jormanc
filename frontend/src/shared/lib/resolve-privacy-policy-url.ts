const DEFAULT_PRIVACY_POLICY_URL = "https://flitsas.com.co/privacy-policy";

/**
 * URL de la política de privacidad (Habeas Data).
 * Configurable vía `VITE_PRIVACY_POLICY_URL`; fallback al dominio oficial FLIT.
 */
export function resolvePrivacyPolicyUrl(): string {
  const configured = import.meta.env.VITE_PRIVACY_POLICY_URL?.trim();
  if (configured) {
    return configured.replace(/\/$/, "");
  }
  return DEFAULT_PRIVACY_POLICY_URL;
}

export const PRIVACY_POLICY_ACCEPTANCE_TEXT =
  "Acepto la política de privacidad y tratamiento de datos (Habeas Data).";
