import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../../shared/api/client.js";
import {
  CreateOtFormSchema,
  CreateOtLabelFormSchema,
  DocumentOrderEntrySchema,
  OtDocumentLabelSchema,
  OtIntegrationLogsPageSchema,
  OtLabelImpactSchema,
  OtOrganismSchema,
  UpdateDocumentOrderResponseSchema,
  UpdateOtFormSchema,
  UpdateOtLabelFormSchema,
  UpdateOtModeSchema,
  type CreateOtFormValues,
  type CreateOtLabelFormValues,
  type OtMode,
  type QuipuxConfigFormValues,
  type UpdateOtFormValues,
  type UpdateOtLabelFormValues,
} from "./ot-admin.schemas.js";
import { z } from "zod";

export const otAdminQueryKeys = {
  list: () => ["ot-organisms", "list"] as const,
  detail: (id: string) => ["ot-organisms", id] as const,
  integrationLogs: (otId: string, page: number) =>
    ["ot-organisms", otId, "integration-logs", page] as const,
  documentOrder: (otId: string) => ["ot-organisms", otId, "document-order"] as const,
  labels: (otId: string) => ["ot-organisms", otId, "labels"] as const,
  labelImpact: (otId: string, labelId: string) =>
    ["ot-organisms", otId, "labels", labelId, "impact"] as const,
};

export function useOtOrganismsList() {
  return useQuery({
    queryKey: otAdminQueryKeys.list(),
    queryFn: async () => {
      const res = await apiClient.get("/ot-organisms");
      return z.array(OtOrganismSchema).parse(res.data);
    },
  });
}

export function useOtOrganism(id: string | null) {
  return useQuery({
    queryKey: otAdminQueryKeys.detail(id ?? ""),
    enabled: !!id,
    queryFn: async () => {
      const res = await apiClient.get(`/ot-organisms/${id}`);
      return OtOrganismSchema.parse(res.data);
    },
  });
}

export function useCreateOtOrganism() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateOtFormValues) => {
      const body = CreateOtFormSchema.parse(data);
      const res = await apiClient.post("/ot-organisms", body);
      return OtOrganismSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["ot-organisms"] }),
  });
}

export function useUpdateOtOrganism(otId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: UpdateOtFormValues) => {
      const body = UpdateOtFormSchema.parse(data);
      const res = await apiClient.put(`/ot-organisms/${otId}`, body);
      return OtOrganismSchema.parse(res.data);
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["ot-organisms"] });
      void qc.invalidateQueries({ queryKey: otAdminQueryKeys.detail(otId) });
    },
  });
}

export function useDeleteOtOrganism() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (otId: string) => {
      await apiClient.delete(`/ot-organisms/${otId}`);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["ot-organisms"] }),
  });
}

export function useUpdateOtMode(otId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (mode: OtMode) => {
      const body = UpdateOtModeSchema.parse({ mode });
      const res = await apiClient.patch(`/ot-organisms/${otId}/mode`, body);
      return OtOrganismSchema.parse(res.data);
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["ot-organisms"] });
      void qc.invalidateQueries({ queryKey: otAdminQueryKeys.detail(otId) });
    },
  });
}

export function useUpdateQuipuxConfig(otId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: QuipuxConfigFormValues) => {
      const res = await apiClient.put(`/ot-organisms/${otId}/quipux-config`, {
        endpoint: data.endpoint,
        webhookToken: data.webhookToken || undefined,
      });
      return OtOrganismSchema.parse(res.data);
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["ot-organisms"] });
      void qc.invalidateQueries({ queryKey: otAdminQueryKeys.detail(otId) });
    },
  });
}

export function useOtIntegrationLogs(otId: string | null, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: otAdminQueryKeys.integrationLogs(otId ?? "", page),
    enabled: !!otId,
    queryFn: async () => {
      const res = await apiClient.get(`/ot-organisms/${otId}/integration-logs`, {
        params: { page, page_size: pageSize },
      });
      return OtIntegrationLogsPageSchema.parse(res.data);
    },
  });
}

export function useOtDocumentOrder(otId: string | null) {
  return useQuery({
    queryKey: otAdminQueryKeys.documentOrder(otId ?? ""),
    enabled: !!otId,
    queryFn: async () => {
      const res = await apiClient.get(`/ot-organisms/${otId}/document-order`);
      return z.array(DocumentOrderEntrySchema).parse(res.data);
    },
  });
}

export function useUpdateDocumentOrder(otId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      procedureTypeId,
      orderedDocumentTypeIds,
    }: {
      procedureTypeId: string;
      orderedDocumentTypeIds: string[];
    }) => {
      const res = await apiClient.put(
        `/ot-organisms/${otId}/document-order/${procedureTypeId}`,
        { orderedDocumentTypeIds },
      );
      return UpdateDocumentOrderResponseSchema.parse(res.data);
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: otAdminQueryKeys.documentOrder(otId) });
    },
  });
}

export function useOtLabels(otId: string | null) {
  return useQuery({
    queryKey: otAdminQueryKeys.labels(otId ?? ""),
    enabled: !!otId,
    queryFn: async () => {
      const res = await apiClient.get(`/ot-organisms/${otId}/labels`);
      return z.array(OtDocumentLabelSchema).parse(res.data);
    },
  });
}

export function useOtLabelImpact(otId: string, labelId: string | null) {
  return useQuery({
    queryKey: otAdminQueryKeys.labelImpact(otId, labelId ?? ""),
    enabled: !!labelId,
    queryFn: async () => {
      const res = await apiClient.get(`/ot-organisms/${otId}/labels/${labelId}/impact`);
      return OtLabelImpactSchema.parse(res.data);
    },
  });
}

export function useCreateOtLabel(otId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateOtLabelFormValues) => {
      const body = CreateOtLabelFormSchema.parse(data);
      const res = await apiClient.post(`/ot-organisms/${otId}/labels`, body);
      return OtDocumentLabelSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: otAdminQueryKeys.labels(otId) }),
  });
}

export function useUpdateOtLabel(otId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      labelId,
      data,
    }: {
      labelId: string;
      data: UpdateOtLabelFormValues;
    }) => {
      const body = UpdateOtLabelFormSchema.parse(data);
      const res = await apiClient.put(`/ot-organisms/${otId}/labels/${labelId}`, body);
      return OtDocumentLabelSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: otAdminQueryKeys.labels(otId) }),
  });
}

export function useDeleteOtLabel(otId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ labelId, confirm }: { labelId: string; confirm: boolean }) => {
      const res = await apiClient.delete(`/ot-organisms/${otId}/labels/${labelId}`, {
        data: { confirm },
      });
      return z.object({ deleted: z.boolean(), affectedAttachments: z.number() }).parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: otAdminQueryKeys.labels(otId) }),
  });
}
