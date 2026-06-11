import { z } from "zod";

export const OtModeSchema = z.enum(["dashboard", "qx"]);

export const OtOrganismSchema = z.object({
  id: z.string().uuid(),
  tenantId: z.string().uuid(),
  slug: z.string(),
  name: z.string(),
  mode: OtModeSchema,
  quipuxEnabled: z.boolean(),
  quipuxConfig: z.string().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export const CreateOtFormSchema = z.object({
  slug: z
    .string()
    .min(2, "El slug debe tener al menos 2 caracteres")
    .regex(/^[a-z0-9-]+$/, "Solo minúsculas, números y guiones"),
  name: z.string().min(2, "El nombre es requerido"),
});

export const UpdateOtFormSchema = z.object({
  name: z.string().min(2, "El nombre es requerido"),
});

export const UpdateOtModeSchema = z.object({
  mode: OtModeSchema,
});

export const QuipuxConfigFormSchema = z.object({
  endpoint: z.string().min(1, "El endpoint es requerido"),
  webhookToken: z.string().min(8, "El token debe tener al menos 8 caracteres").optional(),
});

export const OtIntegrationLogSchema = z.object({
  id: z.string().uuid(),
  otId: z.string().uuid(),
  tenantId: z.string().uuid(),
  eventType: z.string(),
  procedureRef: z.string().nullable().optional(),
  requestPayload: z.string().nullable().optional(),
  responsePayload: z.string().nullable().optional(),
  httpStatus: z.number().nullable().optional(),
  durationMs: z.number().nullable().optional(),
  loggedAt: z.string(),
});

export const OtIntegrationLogsPageSchema = z.object({
  data: z.array(OtIntegrationLogSchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type OtOrganism = z.infer<typeof OtOrganismSchema>;
export type OtMode = z.infer<typeof OtModeSchema>;
export type CreateOtFormValues = z.infer<typeof CreateOtFormSchema>;
export type UpdateOtFormValues = z.infer<typeof UpdateOtFormSchema>;
export type QuipuxConfigFormValues = z.infer<typeof QuipuxConfigFormSchema>;
export type OtIntegrationLog = z.infer<typeof OtIntegrationLogSchema>;

export const DocumentTypeSummarySchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
});

export const OrderedDocumentSchema = z.object({
  orderIndex: z.number(),
  documentType: DocumentTypeSummarySchema,
});

export const DocumentOrderEntrySchema = z.object({
  procedureTypeId: z.string().uuid(),
  orderedDocuments: z.array(OrderedDocumentSchema),
});

export const UpdateDocumentOrderResponseSchema = z.object({
  procedureTypeId: z.string().uuid(),
  orderedDocuments: z.array(OrderedDocumentSchema),
  updated: z.boolean(),
  message: z.string(),
});

export const OtDocumentLabelSchema = z.object({
  id: z.string().uuid(),
  otId: z.string().uuid(),
  slug: z.string(),
  displayName: z.string(),
  isActive: z.boolean(),
  createdAt: z.string(),
});

export const OtLabelImpactSchema = z.object({
  impactCount: z.number(),
});

export const CreateOtLabelFormSchema = z.object({
  slug: z
    .string()
    .min(2, "El slug debe tener al menos 2 caracteres")
    .regex(/^[a-z0-9_]+$/, "Solo minúsculas, números y guiones bajos"),
  displayName: z.string().min(2, "El nombre es requerido"),
});

export const UpdateOtLabelFormSchema = z.object({
  displayName: z.string().min(2, "El nombre es requerido"),
  isActive: z.boolean().optional(),
});

export type DocumentOrderEntry = z.infer<typeof DocumentOrderEntrySchema>;
export type OrderedDocument = z.infer<typeof OrderedDocumentSchema>;
export type OtDocumentLabel = z.infer<typeof OtDocumentLabelSchema>;
export type CreateOtLabelFormValues = z.infer<typeof CreateOtLabelFormSchema>;
export type UpdateOtLabelFormValues = z.infer<typeof UpdateOtLabelFormSchema>;

export function parseQuipuxConfig(raw: string | null | undefined): {
  endpoint: string;
  hasWebhookToken: boolean;
} {
  if (!raw) return { endpoint: "", hasWebhookToken: false };
  try {
    const parsed = JSON.parse(raw) as { endpoint?: string; webhook_token_hash?: string };
    return {
      endpoint: parsed.endpoint ?? "",
      hasWebhookToken: Boolean(parsed.webhook_token_hash),
    };
  } catch {
    return { endpoint: "", hasWebhookToken: false };
  }
}
