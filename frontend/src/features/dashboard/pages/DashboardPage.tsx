import { useMemo, useState } from "react";
import { DateRangeFilter } from "../components/DateRangeFilter.js";
import { ExportExcelButton } from "../components/ExportExcelButton.js";
import { ExportPdfButton } from "../components/ExportPdfButton.js";
import { FamilyPieChart } from "../components/FamilyPieChart.js";
import { ProcedureDetailTable } from "../components/ProcedureDetailTable.js";
import { TopUsersCard } from "../components/TopUsersCard.js";
import {
  useDashboardProcedures,
  useDashboardSummary,
  useDashboardTopUsers,
} from "../api/dashboard.api.js";
import type { DashboardFamily } from "../api/dashboard.schemas.js";
import { dateRangeToIso, defaultDateRange, type DateRangeValue } from "../lib/dateRange.js";

export function DashboardPage() {
  const [dateRange, setDateRange] = useState<DateRangeValue>(defaultDateRange);
  const [selectedFamily, setSelectedFamily] = useState<DashboardFamily | null>(null);
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [detailPage, setDetailPage] = useState(1);
  const pageSize = 20;

  const isoRange = useMemo(() => dateRangeToIso(dateRange), [dateRange]);
  const queryRange = useMemo(
    () =>
      isoRange
        ? {
            from: isoRange.from,
            to: isoRange.to,
            userIds: selectedUserIds.length ? selectedUserIds : undefined,
          }
        : { from: "", to: "", userIds: undefined as string[] | undefined },
    [isoRange, selectedUserIds],
  );

  const summaryQuery = useDashboardSummary(queryRange);
  const topUsersQuery = useDashboardTopUsers(queryRange);
  const proceduresQuery = useDashboardProcedures(
    {
      ...queryRange,
      family: selectedFamily ?? undefined,
      page: detailPage,
      pageSize,
    },
    !!selectedFamily && !!isoRange,
  );

  const byFamily = summaryQuery.data?.summary.byFamily ?? [];
  const total = summaryQuery.data?.summary.total ?? 0;
  const topUsers = topUsersQuery.data?.data ?? [];
  const selectableUsers = topUsersQuery.data?.data ?? [];

  const exportParams = isoRange
    ? {
        from: isoRange.from,
        to: isoRange.to,
        family: selectedFamily ?? undefined,
        userIds: selectedUserIds.length ? selectedUserIds : undefined,
        families: selectedFamily ? [selectedFamily] : undefined,
        includeCharts: true,
      }
    : null;

  function handleDateRangeChange(next: DateRangeValue) {
    setDateRange(next);
    setDetailPage(1);
  }

  function handleFamilySelect(family: DashboardFamily) {
    setSelectedFamily(family);
    setDetailPage(1);
  }

  function handleSelectedUserIdsChange(userIds: string[]) {
    setSelectedUserIds(userIds);
    setDetailPage(1);
  }

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="flit-section-title">
            <i className="pi pi-chart-pie text-flit-primary" aria-hidden="true" />
            Dashboard
          </h1>
          <p className="mt-0.5 text-sm text-flit-muted dark:text-flit-muted-dark">
            Distribución de trámites por familia en el período seleccionado
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <ExportExcelButton params={exportParams} disabled={!isoRange} />
          <ExportPdfButton params={exportParams} disabled={!isoRange} />
        </div>
      </div>

      <DateRangeFilter value={dateRange} onChange={handleDateRangeChange} />

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
        <ProcedureDetailTable
          family={selectedFamily}
          items={proceduresQuery.data?.data ?? []}
          total={proceduresQuery.data?.total ?? 0}
          page={detailPage}
          pageSize={pageSize}
          isLoading={!!selectedFamily && proceduresQuery.isLoading}
          error={proceduresQuery.error as Error | null}
          onPageChange={setDetailPage}
          onRetry={() => void proceduresQuery.refetch()}
        />

        <FamilyPieChart
          byFamily={byFamily}
          total={total}
          isLoading={summaryQuery.isLoading}
          error={summaryQuery.error as Error | null}
          selectedFamily={selectedFamily}
          onFamilySelect={handleFamilySelect}
          onRetry={() => void summaryQuery.refetch()}
        />
      </div>

      <TopUsersCard
        users={topUsers}
        selectableUsers={selectableUsers}
        selectedUserIds={selectedUserIds}
        isLoading={topUsersQuery.isLoading}
        error={topUsersQuery.error as Error | null}
        onSelectedUserIdsChange={handleSelectedUserIdsChange}
        onRetry={() => void topUsersQuery.refetch()}
      />
    </div>
  );
}
