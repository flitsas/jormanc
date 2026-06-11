import { useMutation, useQuery } from "@tanstack/react-query";
import { apiClient } from "../../../shared/api/client.js";
import {
  DashboardProceduresPageSchema,
  DashboardSummarySchema,
  DashboardTopUsersSchema,
  type DashboardFamily,
} from "./dashboard.schemas.js";
import { parseContentDispositionFilename, triggerBlobDownload } from "../lib/downloadBlob.js";

export interface DashboardDateRange {
  from: string;
  to: string;
  userIds?: string[];
}

export interface DashboardProceduresParams extends DashboardDateRange {
  family?: DashboardFamily;
  page?: number;
  pageSize?: number;
}

export interface DashboardExportParams extends DashboardDateRange {
  family?: DashboardFamily;
  families?: DashboardFamily[];
  includeCharts?: boolean;
}

export const dashboardQueryKeys = {
  summary: (range: DashboardDateRange) => ["dashboard", "summary", range] as const,
  procedures: (params: Record<string, unknown>) => ["dashboard", "procedures", params] as const,
  topUsers: (range: DashboardDateRange) => ["dashboard", "top-users", range] as const,
};

function buildUserIdsParam(userIds?: string[]) {
  return userIds?.length ? userIds : undefined;
}

async function fetchDashboardExport(
  path: "/dashboard/export/excel" | "/dashboard/export/pdf",
  params: Record<string, unknown>,
): Promise<{ blob: Blob; filename: string }> {
  const res = await apiClient.get(path, {
    params,
    responseType: "blob",
    timeout: 120_000,
  });
  const disposition = res.headers["content-disposition"] as string | undefined;
  const defaultName = path.includes("excel") ? "dashboard-export.xlsx" : "resumen-ejecutivo.pdf";
  const filename = parseContentDispositionFilename(disposition) ?? defaultName;
  return { blob: res.data as Blob, filename };
}

export function useDashboardSummary(range: DashboardDateRange) {
  return useQuery({
    queryKey: dashboardQueryKeys.summary(range),
    queryFn: async () => {
      const res = await apiClient.get("/dashboard/summary", {
        params: {
          from: range.from,
          to: range.to,
          user_ids: buildUserIdsParam(range.userIds),
        },
      });
      return DashboardSummarySchema.parse(res.data);
    },
    enabled: !!range.from && !!range.to,
  });
}

export function useDashboardProcedures(params: DashboardProceduresParams, enabled = true) {
  const page = params.page ?? 1;
  const pageSize = params.pageSize ?? 20;

  return useQuery({
    queryKey: dashboardQueryKeys.procedures({
      from: params.from,
      to: params.to,
      family: params.family,
      userIds: params.userIds,
      page,
      pageSize,
    }),
    queryFn: async () => {
      const res = await apiClient.get("/dashboard/procedures", {
        params: {
          from: params.from,
          to: params.to,
          family: params.family,
          user_ids: buildUserIdsParam(params.userIds),
          page,
          page_size: pageSize,
        },
      });
      return DashboardProceduresPageSchema.parse(res.data);
    },
    enabled: enabled && !!params.from && !!params.to && !!params.family,
  });
}

export function useDashboardTopUsers(range: DashboardDateRange) {
  return useQuery({
    queryKey: dashboardQueryKeys.topUsers(range),
    queryFn: async () => {
      const res = await apiClient.get("/dashboard/top-users", {
        params: {
          from: range.from,
          to: range.to,
          user_ids: buildUserIdsParam(range.userIds),
        },
      });
      return DashboardTopUsersSchema.parse(res.data);
    },
    enabled: !!range.from && !!range.to,
  });
}

export function useExportDashboardExcel() {
  return useMutation({
    mutationFn: async (params: DashboardExportParams) => {
      const result = await fetchDashboardExport("/dashboard/export/excel", {
        from: params.from,
        to: params.to,
        family: params.family,
        user_ids: buildUserIdsParam(params.userIds),
        format: "xlsx",
      });
      triggerBlobDownload(result.blob, result.filename);
    },
  });
}

export function useExportDashboardPdf() {
  return useMutation({
    mutationFn: async (params: DashboardExportParams) => {
      const result = await fetchDashboardExport("/dashboard/export/pdf", {
        from: params.from,
        to: params.to,
        families: params.families?.length ? params.families : undefined,
        include_charts: params.includeCharts ?? true,
      });
      triggerBlobDownload(result.blob, result.filename);
    },
  });
}
