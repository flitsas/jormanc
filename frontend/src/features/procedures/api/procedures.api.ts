import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../../shared/api/client.js";
import {
  AddActorRequestSchema,
  AddActorResponseSchema,
  AttachmentListSchema,
  AttachmentUploadResponseSchema,
  CaptureVehicleResponseSchema,
  ProcedureDetailSchema,
  ProcedureListPageSchema,
  SecondarySellerListSchema,
  SubmitProcedureResponseSchema,
  type AddActorRequest,
} from "./procedures.schemas.js";
import {
  CUOTA_SUM_ERROR,
  formatCuotaErrorMessage,
  isCuotaSumError,
} from "../lib/cuotaValidation.js";

export class CuotaValidationError extends Error {
  readonly code = CUOTA_SUM_ERROR;
  readonly currentSum: number;
  readonly proposed: number;

  constructor(currentSum: number, proposed: number) {
    super(formatCuotaErrorMessage(currentSum, proposed));
    this.name = "CuotaValidationError";
    this.currentSum = currentSum;
    this.proposed = proposed;
  }
}

export interface ProceduresListParams {
  page?: number;
  pageSize?: number;
  status?: string;
  fechaFrom?: string;
}

export const proceduresQueryKeys = {
  list: (params: Record<string, unknown>) => ["procedures", "list", params] as const,
  detail: (id: string) => ["procedures", "detail", id] as const,
  attachments: (id: string) => ["procedures", "attachments", id] as const,
  secondarySellers: (id: string) => ["procedures", "secondary-sellers", id] as const,
};

export function useProceduresList(params: ProceduresListParams = {}) {
  const page = params.page ?? 1;
  const pageSize = params.pageSize ?? 20;

  return useQuery({
    queryKey: proceduresQueryKeys.list({
      page,
      pageSize,
      status: params.status,
      fechaFrom: params.fechaFrom,
    }),
    queryFn: async () => {
      const res = await apiClient.get("/procedures", {
        params: {
          page,
          page_size: pageSize,
          status: params.status || undefined,
          fecha_from: params.fechaFrom || undefined,
        },
      });
      return ProcedureListPageSchema.parse(res.data);
    },
  });
}

export function useProcedureDetail(procedureId: string | undefined) {
  return useQuery({
    queryKey: proceduresQueryKeys.detail(procedureId ?? ""),
    enabled: !!procedureId,
    queryFn: async () => {
      const res = await apiClient.get(`/procedures/${procedureId}`);
      return ProcedureDetailSchema.parse(res.data);
    },
  });
}

export function useCaptureVehicle(procedureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { plate?: string; vin?: string }) => {
      const res = await apiClient.patch(`/procedures/${procedureId}/vehicle`, payload);
      return CaptureVehicleResponseSchema.parse(res.data);
    },
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: proceduresQueryKeys.detail(procedureId) }),
  });
}

export function useAddActor(procedureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: AddActorRequest) => {
      const body = AddActorRequestSchema.parse(payload);
      try {
        const res = await apiClient.post(`/procedures/${procedureId}/actors`, body);
        return AddActorResponseSchema.parse(res.data);
      } catch (err) {
        const enriched = err as Error & { status?: number; data?: unknown };
        if (enriched.status === 422 && isCuotaSumError(enriched.data)) {
          throw new CuotaValidationError(enriched.data.current_sum, enriched.data.proposed);
        }
        throw err;
      }
    },
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: proceduresQueryKeys.detail(procedureId) }),
  });
}

export function useSubmitProcedure(procedureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const res = await apiClient.post(`/procedures/${procedureId}/submit`);
      return SubmitProcedureResponseSchema.parse(res.data);
    },
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: proceduresQueryKeys.detail(procedureId) }),
  });
}

export function useProcedureAttachments(procedureId: string | undefined) {
  return useQuery({
    queryKey: proceduresQueryKeys.attachments(procedureId ?? ""),
    enabled: !!procedureId,
    queryFn: async () => {
      const res = await apiClient.get(`/procedures/${procedureId}/attachments`);
      return AttachmentListSchema.parse(res.data);
    },
  });
}

export function useUploadAttachment(procedureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { file: File; labelSlug: string }) => {
      const formData = new FormData();
      formData.append("file", payload.file);
      formData.append("label_slug", payload.labelSlug);
      const res = await apiClient.post(`/procedures/${procedureId}/attachments`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      return AttachmentUploadResponseSchema.parse(res.data);
    },
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: proceduresQueryKeys.attachments(procedureId) }),
  });
}

export function useSecondarySellers(procedureId: string | undefined) {
  return useQuery({
    queryKey: proceduresQueryKeys.secondarySellers(procedureId ?? ""),
    enabled: !!procedureId,
    queryFn: async () => {
      const res = await apiClient.get(`/procedures/${procedureId}/secondary-sellers`);
      return SecondarySellerListSchema.parse(res.data);
    },
  });
}
