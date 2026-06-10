import { describe, expect, it, vi, afterEach } from "vitest";
import {
  PRIVACY_POLICY_ACCEPTANCE_TEXT,
  resolvePrivacyPolicyUrl,
} from "./resolve-privacy-policy-url.js";

describe("resolvePrivacyPolicyUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns default URL when env is unset", () => {
    vi.stubEnv("VITE_PRIVACY_POLICY_URL", "");
    expect(resolvePrivacyPolicyUrl()).toBe("https://flitsas.com.co/privacy-policy");
  });

  it("returns configured URL without trailing slash", () => {
    vi.stubEnv("VITE_PRIVACY_POLICY_URL", "https://ejemplo-qa.flit.test/privacidad/");
    expect(resolvePrivacyPolicyUrl()).toBe("https://ejemplo-qa.flit.test/privacidad");
  });

  it("exposes stable acceptance copy for checkbox aria-label", () => {
    expect(PRIVACY_POLICY_ACCEPTANCE_TEXT).toContain("Habeas Data");
  });
});
