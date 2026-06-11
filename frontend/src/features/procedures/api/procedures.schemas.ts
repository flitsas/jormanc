import { z } from "zod";
import {
  FormFieldSchema,
  FormSectionSchema,
  ProcedureStepSchema,
} from "../../procedures-config/api/procedures-config.schemas.js";

export const SnapshotStepSchema = ProcedureStepSchema.omit({ procedureTypeId: true });

export const SnapshotConfigSchema = z.object({
  steps: z.array(SnapshotStepSchema),
});

export const ProcedureDetailSchema = z.object({
  id: z.string().uuid(),
  compositeId: z.string(),
  status: z.string(),
  procedureTypeSnapshotId: z.string().uuid(),
  vehicleQueryKey: z.string(),
  currentStepOrder: z.number(),
  stepData: z.record(z.unknown()).default({}),
  snapshotConfig: SnapshotConfigSchema,
});

export const ProcedureListItemSchema = z.object({
  id: z.string().uuid(),
  compositeId: z.string(),
  status: z.string(),
  procedureTypeId: z.string().uuid(),
  companyId: z.string().uuid(),
  createdAt: z.string(),
});

export const ProcedureListPageSchema = z.object({
  data: z.array(ProcedureListItemSchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const VehicleSummarySchema = z.object({
  plate: z.string().nullable().optional(),
  vin: z.string().nullable().optional(),
  brand: z.string().nullable().optional(),
  model: z.string().nullable().optional(),
  year: z.number().nullable().optional(),
  color: z.string().nullable().optional(),
  ownerName: z.string().nullable().optional(),
});

export const CaptureVehicleResponseSchema = z.object({
  procedureId: z.string().uuid(),
  status: z.string(),
  vehicle: VehicleSummarySchema,
  warnings: z.array(z.string()),
});

export const AddActorResponseSchema = z.object({
  actorId: z.string().uuid(),
  nature: z.string(),
  queryResults: z.unknown().nullable().optional(),
  warnings: z.array(z.string()),
  legalRepresentativeActorId: z.string().uuid().nullable().optional(),
});

export const AddActorRequestSchema = z.object({
  actorDefinitionId: z.string().uuid(),
  nature: z.enum(["natural", "juridica"]),
  documentType: z.string().optional(),
  documentNumber: z.string().optional(),
  nit: z.string().optional(),
  cuotaPct: z.number().min(0).max(100).optional(),
});

export const SubmitProcedureResponseSchema = z.object({
  id: z.string().uuid(),
  status: z.string(),
  submittedAt: z.string().nullable().optional(),
});

export const AttachmentListItemSchema = z.object({
  id: z.string().uuid(),
  fileName: z.string(),
  labelSlug: z.string(),
  sizeBytes: z.number(),
  uploadedAt: z.string(),
});

export const AttachmentListSchema = z.array(AttachmentListItemSchema);

export const AttachmentUploadResponseSchema = z.object({
  attachment_id: z.string().uuid(),
  file_name: z.string(),
  label_slug: z.string(),
});

export const SecondarySellerSchema = z.object({
  documentType: z.string(),
  documentNumber: z.string(),
  fullName: z.string(),
  ownershipPct: z.number().nullable().optional(),
});

export const SecondarySellerListSchema = z.array(SecondarySellerSchema);

export type ProcedureDetail = z.infer<typeof ProcedureDetailSchema>;
export type SnapshotStep = z.infer<typeof SnapshotStepSchema>;
export type SnapshotConfig = z.infer<typeof SnapshotConfigSchema>;
export type ProcedureListItem = z.infer<typeof ProcedureListItemSchema>;
export type AddActorRequest = z.infer<typeof AddActorRequestSchema>;
export type AttachmentListItem = z.infer<typeof AttachmentListItemSchema>;
export type SecondarySeller = z.infer<typeof SecondarySellerSchema>;
export type FormField = z.infer<typeof FormFieldSchema>;
export type FormSection = z.infer<typeof FormSectionSchema>;
