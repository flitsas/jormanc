import { z } from "zod";

export const DocumentTemplateSchema = z.object({
  templateId: z.string().uuid(),
  documentTypeId: z.string().uuid(),
  version: z.number().int().positive(),
  status: z.enum(["active", "deprecated"]),
  contentRef: z.string(),
  notes: z.string().nullable().optional(),
  markersDetected: z.array(z.string()),
  createdAt: z.string(),
});

export const DocumentTemplateDetailSchema = DocumentTemplateSchema.extend({
  htmlContent: z.string(),
});

export type DocumentTemplate = z.infer<typeof DocumentTemplateSchema>;
export type DocumentTemplateDetail = z.infer<typeof DocumentTemplateDetailSchema>;

export const DocumentLoadTypeSchema = z.enum(["carga", "generacion"]);
export const DocumentOriginSchema = z.enum(["generated", "uploaded"]);
export const DocumentStatusSchema = z.enum(["pending", "ready", "failed", "expired"]);

export const ProcedureDocumentTypeSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  loadType: DocumentLoadTypeSchema,
});

export const ProcedureDocumentItemSchema = z.object({
  id: z.string().uuid(),
  documentType: ProcedureDocumentTypeSchema,
  origin: DocumentOriginSchema,
  status: DocumentStatusSchema,
  templateVersion: z.number().nullable().optional(),
  fileRef: z.string().nullable().optional(),
  generatedAt: z.string().nullable().optional(),
  uploadedBy: z.string().uuid().nullable().optional(),
  isRequired: z.boolean(),
  orderIndex: z.number(),
});

export const ConsolidatedPackageSchema = z.object({
  version: z.number(),
  mergedFileRef: z.string(),
  createdAt: z.string(),
  downloadFilename: z.string(),
  docCount: z.number(),
});

export const ProcedureDocumentsStatusSchema = z.object({
  procedureId: z.string().uuid(),
  documents: z.array(ProcedureDocumentItemSchema),
  consolidatedPackages: z.array(ConsolidatedPackageSchema),
});

export const ConsolidateDocumentsResponseSchema = ConsolidatedPackageSchema;

export type DocumentLoadType = z.infer<typeof DocumentLoadTypeSchema>;
export type DocumentOrigin = z.infer<typeof DocumentOriginSchema>;
export type DocumentStatus = z.infer<typeof DocumentStatusSchema>;
export type ProcedureDocumentItem = z.infer<typeof ProcedureDocumentItemSchema>;
export type ConsolidatedPackage = z.infer<typeof ConsolidatedPackageSchema>;
export type ProcedureDocumentsStatus = z.infer<typeof ProcedureDocumentsStatusSchema>;
