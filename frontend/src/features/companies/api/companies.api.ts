import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../../shared/api/client.js";
import {
  CompanyPageSchema,
  CompanySchema,
  CompanyConfigSchema,
  SignatureMatrixEntrySchema,
  UserExceptionSchema,
  IntegrationLogsPageSchema,
  type CreateCompanyFormValues,
} from "./companies.schemas.js";
import { z } from "zod";

export const companiesQueryKeys = {
  list: (filters: Record<string, unknown>) => ["companies", "list", filters] as const,
  signatureMatrix: (companyId: string) => ["companies", companyId, "signature-matrix"] as const,
  userExceptions: (companyId: string) => ["companies", companyId, "user-exceptions"] as const,
  integrationLogs: (tenantId: string, page: number) =>
    ["integration-logs", tenantId, page] as const,
};

export function useCompaniesList(params: {
  page: number;
  pageSize: number;
  nit?: string;
  name?: string;
}) {
  return useQuery({
    queryKey: companiesQueryKeys.list(params),
    queryFn: async () => {
      const res = await apiClient.get("/admin/companies/index", {
        params: {
          page: params.page,
          page_size: params.pageSize,
          nit: params.nit || undefined,
          name: params.name || undefined,
        },
      });
      return CompanyPageSchema.parse(res.data);
    },
  });
}

export function useCreateCompany() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateCompanyFormValues) => {
      const res = await apiClient.post("/admin/companies", data);
      return CompanySchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["companies"] }),
  });
}

export function useUpdateCompanyConfig(companyId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (patch: Record<string, unknown>) => {
      const res = await apiClient.patch(`/admin/companies/${companyId}/config`, patch);
      return CompanyConfigSchema.parse(res.data);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["companies"] }),
  });
}

export function useSignatureMatrix(companyId: string | null) {
  return useQuery({
    queryKey: companiesQueryKeys.signatureMatrix(companyId ?? ""),
    enabled: !!companyId,
    queryFn: async () => {
      const res = await apiClient.get(`/admin/companies/${companyId}/config/signature-matrix`);
      return z.array(SignatureMatrixEntrySchema).parse(res.data);
    },
  });
}

export function useUpdateSignatureMatrix(companyId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (entries: { actorRole: string; signatureType: string }[]) => {
      const res = await apiClient.put(`/admin/companies/${companyId}/config/signature-matrix`, {
        entries: entries.map((e) => ({
          actorRole: e.actorRole,
          signatureType: e.signatureType,
          isActive: true,
        })),
      });
      return z.array(SignatureMatrixEntrySchema).parse(res.data);
    },
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: companiesQueryKeys.signatureMatrix(companyId) }),
  });
}

export function useUserExceptions(companyId: string | null) {
  return useQuery({
    queryKey: companiesQueryKeys.userExceptions(companyId ?? ""),
    enabled: !!companyId,
    queryFn: async () => {
      const res = await apiClient.get(`/admin/companies/${companyId}/user-exceptions`);
      return z.array(UserExceptionSchema).parse(res.data);
    },
  });
}

export function useAddUserException(companyId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (userId: string) => {
      const res = await apiClient.post(`/admin/companies/${companyId}/user-exceptions`, {
        userId,
      });
      return UserExceptionSchema.parse(res.data);
    },
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: companiesQueryKeys.userExceptions(companyId) }),
  });
}

export function useRemoveUserException(companyId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (userId: string) => {
      await apiClient.delete(`/admin/companies/${companyId}/user-exceptions/${userId}`);
    },
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: companiesQueryKeys.userExceptions(companyId) }),
  });
}

export function useIntegrationLogs(tenantId: string | null, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: companiesQueryKeys.integrationLogs(tenantId ?? "", page),
    enabled: !!tenantId,
    queryFn: async () => {
      const res = await apiClient.get("/admin/integration-logs", {
        params: { tenant_id: tenantId, page, page_size: pageSize },
      });
      return IntegrationLogsPageSchema.parse(res.data);
    },
  });
}
