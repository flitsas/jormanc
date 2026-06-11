import { z } from "zod";

export const DashboardFamilySchema = z.enum(["matricula_inicial", "traspasos", "otros"]);
export type DashboardFamily = z.infer<typeof DashboardFamilySchema>;

export const StatusBreakdownSchema = z.object({
  draft: z.number(),
  submitted: z.number(),
  approved: z.number(),
  rejected: z.number(),
});

export const FamilySummarySchema = z.object({
  family: DashboardFamilySchema,
  count: z.number(),
  pct: z.number(),
  byStatus: StatusBreakdownSchema,
});

export const DashboardSummarySchema = z.object({
  period: z.object({
    from: z.string(),
    to: z.string(),
  }),
  summary: z.object({
    total: z.number(),
    byFamily: z.array(FamilySummarySchema),
    byStatus: StatusBreakdownSchema,
  }),
});

export const DashboardProcedureItemSchema = z.object({
  id: z.string(),
  submittedAt: z.string().nullable().optional(),
  status: z.string(),
  plate: z.string().nullable().optional(),
  ownerName: z.string().nullable().optional(),
  approvedAt: z.string().nullable().optional(),
  updatedAt: z.string(),
});

export const DashboardProceduresPageSchema = z.object({
  data: z.array(DashboardProcedureItemSchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const TopUserSchema = z.object({
  userId: z.string().uuid(),
  fullName: z.string(),
  count: z.number(),
  pctOfTotal: z.number(),
});

export const DashboardTopUsersSchema = z.object({
  data: z.array(TopUserSchema),
});

export type StatusBreakdown = z.infer<typeof StatusBreakdownSchema>;
export type FamilySummary = z.infer<typeof FamilySummarySchema>;
export type DashboardSummary = z.infer<typeof DashboardSummarySchema>;
export type DashboardProcedureItem = z.infer<typeof DashboardProcedureItemSchema>;
export type DashboardProceduresPage = z.infer<typeof DashboardProceduresPageSchema>;
export type TopUser = z.infer<typeof TopUserSchema>;
export type DashboardTopUsers = z.infer<typeof DashboardTopUsersSchema>;
