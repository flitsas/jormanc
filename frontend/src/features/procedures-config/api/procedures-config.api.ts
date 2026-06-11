import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../../shared/api/client.js";
import {
  ActorDefinitionSchema,
  ApiConnectorSchema,
  CoherenceSimulationSchema,
  FormFieldSchema,
  ProcedureStepSchema,
  ProcedureTypeSchema,
  QueryRuleSchema,
  RuleSetSchema,
  type FormField,
} from "./procedures-config.schemas.js";
import type { VerificationToggle } from "../components/QueryRulesEditor.js";

export const proceduresConfigKeys = {
  detail: (id: string) => ["procedure-types", id] as const,
};

export function useProcedureType(id: string | undefined) {
  return useQuery({
    queryKey: proceduresConfigKeys.detail(id ?? ""),
    enabled: !!id,
    queryFn: async () => {
      const res = await apiClient.get(`/procedure-types/${id}`);
      return ProcedureTypeSchema.parse(res.data);
    },
  });
}

export function useUpdateProcedureStep(procedureTypeId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      stepId,
      orderIndex,
      name,
    }: {
      stepId: string;
      orderIndex: number;
      name?: string;
    }) => {
      const res = await apiClient.put(`/procedure-types/${procedureTypeId}/steps/${stepId}`, {
        orderIndex,
        name,
      });
      return ProcedureStepSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: proceduresConfigKeys.detail(procedureTypeId) }),
  });
}

export function useUpdateFormField(procedureTypeId: string, stepId: string, sectionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      fieldId,
      patch,
    }: {
      fieldId: string;
      patch: Partial<Pick<FormField, "name" | "fieldType" | "isRequired" | "config">>;
    }) => {
      const res = await apiClient.put(
        `/procedure-types/${procedureTypeId}/steps/${stepId}/sections/${sectionId}/fields/${fieldId}`,
        {
          name: patch.name,
          fieldType: patch.fieldType,
          isRequired: patch.isRequired,
          config: patch.config,
        },
      );
      return FormFieldSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: proceduresConfigKeys.detail(procedureTypeId) }),
  });
}

export function useUpdateApiConnector(procedureTypeId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      connectorId,
      paramBindings,
    }: {
      connectorId: string;
      paramBindings: Record<string, string>;
    }) => {
      const res = await apiClient.put(
        `/procedure-types/${procedureTypeId}/api-connectors/${connectorId}`,
        { paramBindings },
      );
      return ApiConnectorSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: proceduresConfigKeys.detail(procedureTypeId) }),
  });
}

export function useSimulateCoherence(procedureTypeId: string) {
  return useMutation({
    mutationFn: async (payload: {
      name: string;
      conditions: Record<string, unknown>;
      actions: Array<Record<string, unknown>>;
    }) => {
      const res = await apiClient.post(`/procedure-types/${procedureTypeId}/rules/simulate`, payload);
      return CoherenceSimulationSchema.parse(res.data);
    },
  });
}

export function useCreateRuleSet(procedureTypeId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      name: string;
      conditions: Record<string, unknown>;
      actions: Array<Record<string, unknown>>;
    }) => {
      const res = await apiClient.post(`/procedure-types/${procedureTypeId}/rules`, payload);
      return RuleSetSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: proceduresConfigKeys.detail(procedureTypeId) }),
  });
}

export function useCreateActor(procedureTypeId: string) {
  return useMutation({
    mutationFn: async (payload: {
      role: string;
      allowedNature?: string;
      minCount?: number;
      maxCount?: number;
      legalRepActorId?: string;
    }) => {
      const res = await apiClient.post(`/procedure-types/${procedureTypeId}/actors`, payload);
      return ActorDefinitionSchema.parse(res.data);
    },
  });
}

export function useCreateQueryRule(procedureTypeId: string) {
  return useMutation({
    mutationFn: async (payload: {
      actorId: string;
      subjectType: string;
      entryKey: string;
      isBlocking: boolean;
      verifications: VerificationToggle[];
    }) => {
      const res = await apiClient.post(
        `/procedure-types/${procedureTypeId}/actors/${payload.actorId}/query-rules`,
        {
          subjectType: payload.subjectType,
          entryKey: payload.entryKey,
          isBlocking: payload.isBlocking,
          verifications: payload.verifications.map((v) => ({
            type: v.type,
            is_active: v.isActive,
            order: v.order,
          })),
        },
      );
      return QueryRuleSchema.parse(res.data);
    },
  });
}
