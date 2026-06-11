import { z } from "zod";

export const DropdownOptionSchema = z.object({
  value: z.string(),
  label: z.string(),
});

export const FormFieldSchema = z.object({
  id: z.string().uuid(),
  sectionId: z.string().uuid(),
  orderIndex: z.number(),
  slug: z.string(),
  name: z.string(),
  fieldType: z.string(),
  isRequired: z.boolean(),
  config: z.record(z.unknown()).or(z.array(z.unknown())).optional().default({}),
});

export const FormSectionSchema = z.object({
  id: z.string().uuid(),
  stepId: z.string().uuid(),
  orderIndex: z.number(),
  slug: z.string(),
  name: z.string(),
  isCollapsible: z.boolean(),
  fields: z.array(FormFieldSchema),
});

export const ProcedureStepSchema = z.object({
  id: z.string().uuid(),
  procedureTypeId: z.string().uuid(),
  orderIndex: z.number(),
  name: z.string(),
  stepType: z.string(),
  isRequired: z.boolean(),
  sections: z.array(FormSectionSchema),
});

export const ApiConnectorSchema = z.object({
  id: z.string().uuid(),
  procedureTypeId: z.string().uuid(),
  tenantId: z.string().uuid(),
  name: z.string(),
  endpoint: z.string(),
  httpVerb: z.string(),
  stepOrder: z.number(),
  paramBindings: z.record(z.string()),
  responseMappings: z.record(z.unknown()).optional().default({}),
  isActive: z.boolean(),
  createdAt: z.string(),
});

export const ProcedureTypeSchema = z.object({
  id: z.string().uuid(),
  tenantId: z.string().uuid(),
  slug: z.string(),
  name: z.string(),
  family: z.string(),
  scope: z.string(),
  scopeRefId: z.string().uuid().nullable().optional(),
  vehicleQueryKey: z.string(),
  version: z.number(),
  isActive: z.boolean(),
  createdAt: z.string(),
  steps: z.array(ProcedureStepSchema),
  apiConnectors: z.array(ApiConnectorSchema).default([]),
});

export const CoherenceConflictSchema = z.object({
  rule1: z.string(),
  rule2: z.string(),
  description: z.string(),
});

export const CoherenceSimulationSchema = z.object({
  isCoherent: z.boolean(),
  conflicts: z.array(CoherenceConflictSchema),
});

export const RuleSetSchema = z.object({
  id: z.string().uuid(),
  procedureTypeId: z.string().uuid(),
  tenantId: z.string().uuid(),
  name: z.string(),
  conditions: z.record(z.unknown()),
  actions: z.array(z.record(z.unknown())),
  isActive: z.boolean(),
  createdAt: z.string(),
});

export const ActorDefinitionSchema = z.object({
  id: z.string().uuid(),
  procedureTypeId: z.string().uuid(),
  tenantId: z.string().uuid(),
  role: z.string(),
  allowedNature: z.string(),
  minCount: z.number(),
  maxCount: z.number(),
  isRequired: z.boolean(),
  orderIndex: z.number(),
  legalRepActorId: z.string().uuid().nullable().optional(),
  createdAt: z.string(),
});

export const QueryRuleSchema = z.object({
  id: z.string().uuid(),
  actorDefinitionId: z.string().uuid(),
  tenantId: z.string().uuid(),
  subjectType: z.string(),
  entryKey: z.string(),
  isBlocking: z.boolean(),
  verifications: z.array(
    z.object({
      type: z.string(),
      isActive: z.boolean(),
      order: z.number(),
    }),
  ),
  createdAt: z.string(),
});

export type ProcedureType = z.infer<typeof ProcedureTypeSchema>;
export type ActorDefinition = z.infer<typeof ActorDefinitionSchema>;
export type CoherenceSimulation = z.infer<typeof CoherenceSimulationSchema>;
export type ProcedureStep = z.infer<typeof ProcedureStepSchema>;
export type FormSection = z.infer<typeof FormSectionSchema>;
export type FormField = z.infer<typeof FormFieldSchema>;
export type ApiConnector = z.infer<typeof ApiConnectorSchema>;
export type DropdownOption = z.infer<typeof DropdownOptionSchema>;
