import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { apiClient, TOKEN_KEY } from "../../../shared/api/client.js";
import {
  ConsolidateDocumentsResponseSchema,
  DocumentTemplateDetailSchema,
  DocumentTemplateSchema,
  ProcedureDocumentsStatusSchema,
  type DocumentTemplate,
} from "./documents.schemas.js";

export const documentsQueryKeys = {
  templates: (documentTypeId: string) => ["document-templates", documentTypeId] as const,
  templateDetail: (documentTypeId: string, versionId: string) =>
    ["document-templates", documentTypeId, versionId] as const,
  status: (procedureId: string) => ["documents", "status", procedureId] as const,
};

export function useDocumentTemplates(documentTypeId: string | null) {
  return useQuery({
    queryKey: documentsQueryKeys.templates(documentTypeId ?? ""),
    enabled: !!documentTypeId,
    queryFn: async () => {
      const res = await apiClient.get(`/document-types/${documentTypeId}/templates`);
      return z.array(DocumentTemplateSchema).parse(res.data);
    },
  });
}

export function useDocumentTemplateDetail(documentTypeId: string | null, versionId: string | null) {
  return useQuery({
    queryKey: documentsQueryKeys.templateDetail(documentTypeId ?? "", versionId ?? ""),
    enabled: !!documentTypeId && !!versionId,
    queryFn: async () => {
      const res = await apiClient.get(`/document-types/${documentTypeId}/templates/${versionId}`);
      return DocumentTemplateDetailSchema.parse(res.data);
    },
  });
}

export interface UploadTemplateInput {
  documentTypeId: string;
  file: File;
  notes?: string;
}

export function useUploadDocumentTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ documentTypeId, file, notes }: UploadTemplateInput) => {
      const formData = new FormData();
      formData.append("html_content", file);
      if (notes?.trim()) formData.append("notes", notes.trim());

      const res = await apiClient.post(`/document-types/${documentTypeId}/templates`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      return DocumentTemplateSchema.parse(res.data);
    },
    onSuccess: (_data, variables) => {
      void qc.invalidateQueries({
        queryKey: documentsQueryKeys.templates(variables.documentTypeId),
      });
    },
  });
}

export function usePreviewTemplatePdf(documentTypeId: string) {
  return useMutation({
    mutationFn: async (versionId: string) => {
      const res = await apiClient.post(
        `/document-types/${documentTypeId}/templates/${versionId}/preview-pdf`,
        {},
        { responseType: "blob" },
      );
      return res.data as Blob;
    },
  });
}

export function sortTemplatesByVersionDesc(templates: DocumentTemplate[]): DocumentTemplate[] {
  return [...templates].sort((a, b) => b.version - a.version);
}

export function useProcedureDocuments(procedureId: string | undefined) {
  return useQuery({
    queryKey: documentsQueryKeys.status(procedureId ?? ""),
    enabled: !!procedureId,
    queryFn: async () => {
      const res = await apiClient.get(`/procedures/${procedureId}/documents`);
      return ProcedureDocumentsStatusSchema.parse(res.data);
    },
  });
}

export function useConsolidateDocuments(procedureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const res = await apiClient.post(`/procedures/${procedureId}/documents/consolidate`);
      return ConsolidateDocumentsResponseSchema.parse(res.data);
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: documentsQueryKeys.status(procedureId) });
    },
  });
}

export async function downloadConsolidatedPdf(
  procedureId: string,
): Promise<{ blob: Blob; filename: string }> {
  const res = await apiClient.get(`/procedures/${procedureId}/consolidated`, {
    responseType: "blob",
    timeout: 60_000,
  });

  const disposition = res.headers["content-disposition"] as string | undefined;
  const filenameMatch = disposition?.match(/filename="?([^";\n]+)"?/i);
  const filename = filenameMatch?.[1] ?? `TRAMITE_${procedureId}.pdf`;

  return { blob: res.data as Blob, filename };
}

export async function fetchConsolidatedPdfBlob(procedureId: string): Promise<Blob> {
  const token = localStorage.getItem(TOKEN_KEY);
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? "/api/v1";
  const res = await fetch(`${baseUrl}/procedures/${procedureId}/consolidated`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  if (!res.ok) {
    const message =
      res.status === 404 ? "Paquete consolidado no disponible." : "Error al cargar el PDF.";
    throw new Error(message);
  }
  return res.blob();
}
