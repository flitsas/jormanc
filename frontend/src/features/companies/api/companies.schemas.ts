import { z } from "zod";

export const CompanyListItemSchema = z.object({
  id: z.string().uuid(),
  tenantId: z.string().uuid(),
  nit: z.string(),
  name: z.string(),
  status: z.string(),
  tenantSlug: z.string(),
  createdAt: z.string(),
});

export const CompanyPageSchema = z.object({
  data: z.array(CompanyListItemSchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const CompanySchema = z.object({
  id: z.string().uuid(),
  tenantId: z.string().uuid(),
  tenantSlug: z.string(),
  nit: z.string(),
  name: z.string(),
  status: z.string(),
  createdAt: z.string(),
});

export const CompanyConfigSchema = z.object({
  id: z.string().uuid(),
  companyId: z.string().uuid(),
  onlyOwnVehicles: z.boolean(),
  baulFirmasEnabled: z.boolean(),
  notificationTarget: z.string(),
  smtpMode: z.string(),
  matriculaConfig: z.string().optional(),
  traspasosConfig: z.string().optional(),
  contingencyConfig: z.string().optional(),
  recaudoMethods: z.string().optional(),
});

export const SignatureMatrixEntrySchema = z.object({
  id: z.string().uuid(),
  companyId: z.string().uuid(),
  actorRole: z.string(),
  signatureType: z.string(),
  isActive: z.boolean(),
});

export const UserExceptionSchema = z.object({
  id: z.string().uuid(),
  companyId: z.string().uuid(),
  userId: z.string().uuid(),
  addedBy: z.string().uuid().nullable().optional(),
  addedAt: z.string(),
});

export const IntegrationLogSchema = z.object({
  id: z.string().uuid(),
  tenantId: z.string().uuid(),
  connectorType: z.string(),
  operation: z.string(),
  provider: z.string(),
  requestPayload: z.string().nullable().optional(),
  responsePayload: z.string().nullable().optional(),
  httpStatus: z.number().nullable().optional(),
  durationMs: z.number(),
  errorMessage: z.string().nullable().optional(),
  loggedAt: z.string(),
});

export const IntegrationLogsPageSchema = z.object({
  data: z.array(IntegrationLogSchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type CompanyListItem = z.infer<typeof CompanyListItemSchema>;
export type Company = z.infer<typeof CompanySchema>;
export type CompanyConfig = z.infer<typeof CompanyConfigSchema>;
export type SignatureMatrixEntry = z.infer<typeof SignatureMatrixEntrySchema>;
export type UserException = z.infer<typeof UserExceptionSchema>;
export type IntegrationLog = z.infer<typeof IntegrationLogSchema>;

export const CreateCompanyFormSchema = z.object({
  nit: z.string().min(5, "NIT requerido"),
  name: z.string().min(2, "Nombre requerido"),
  tenantSlug: z
    .string()
    .min(2, "Slug requerido")
    .regex(/^[a-z0-9-]+$/, "Solo minúsculas, números y guiones"),
});

export type CreateCompanyFormValues = z.infer<typeof CreateCompanyFormSchema>;
