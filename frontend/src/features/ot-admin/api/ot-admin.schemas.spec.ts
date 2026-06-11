import { z } from "zod";
import { describe, expect, it } from "vitest";
import {
  CreateOtFormSchema,
  CreateOtLabelFormSchema,
  DocumentOrderEntrySchema,
  OtDocumentLabelSchema,
  OtOrganismSchema,
  OtIntegrationLogsPageSchema,
  OtLabelImpactSchema,
  parseQuipuxConfig,
} from "./ot-admin.schemas.js";

describe("ot-admin.schemas", () => {
  it("parses OT organism response", () => {
    const parsed = OtOrganismSchema.parse({
      id: "00000000-0000-0000-0000-000000000001",
      tenantId: "00000000-0000-0000-0000-000000000099",
      slug: "ot-bogota",
      name: "Secretaría Bogotá",
      mode: "dashboard",
      quipuxEnabled: false,
      quipuxConfig: null,
      createdAt: "2026-01-01T00:00:00Z",
      updatedAt: "2026-01-01T00:00:00Z",
    });
    expect(parsed.slug).toBe("ot-bogota");
  });

  it("validates create form slug format", () => {
    const result = CreateOtFormSchema.safeParse({ slug: "Invalid Slug!", name: "Test" });
    expect(result.success).toBe(false);
  });

  it("parses integration logs page", () => {
    const parsed = OtIntegrationLogsPageSchema.parse({
      data: [],
      total: 0,
      page: 1,
      pageSize: 20,
    });
    expect(parsed.total).toBe(0);
  });

  it("parseQuipuxConfig extracts endpoint and token flag", () => {
    const parsed = parseQuipuxConfig('{"endpoint":"https://qx.test","webhook_token_hash":"x"}');
    expect(parsed.endpoint).toBe("https://qx.test");
    expect(parsed.hasWebhookToken).toBe(true);
  });

  it("parses document order entry", () => {
    const parsed = DocumentOrderEntrySchema.parse({
      procedureTypeId: "00000000-0000-0000-0000-000000000099",
      orderedDocuments: [
        {
          orderIndex: 1,
          documentType: { id: "00000000-0000-0000-0000-000000000011", name: "SOAT" },
        },
      ],
    });
    expect(parsed.orderedDocuments).toHaveLength(1);
  });

  it("parses OT document label", () => {
    const parsed = OtDocumentLabelSchema.parse({
      id: "00000000-0000-0000-0000-000000000021",
      otId: "00000000-0000-0000-0000-000000000001",
      slug: "paz_y_salvo",
      displayName: "Paz y Salvo",
      isActive: true,
      createdAt: "2026-01-01T00:00:00Z",
    });
    expect(parsed.slug).toBe("paz_y_salvo");
  });

  it("validates create label slug format", () => {
    const result = CreateOtLabelFormSchema.safeParse({
      slug: "Invalid Slug",
      displayName: "Test",
    });
    expect(result.success).toBe(false);
  });

  it("parses label impact response", () => {
    const parsed = OtLabelImpactSchema.parse({ impactCount: 23 });
    expect(parsed.impactCount).toBe(23);
  });
});
