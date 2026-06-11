import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { IntegrationLogsTable } from "./IntegrationLogsTable.js";

vi.mock("../api/companies.api.js", () => ({
  useIntegrationLogs: () => ({
    data: {
      data: [
        {
          id: "00000000-0000-0000-0000-000000000001",
          tenantId: "00000000-0000-0000-0000-000000000099",
          connectorType: "runt",
          operation: "query_plate",
          provider: "mock",
          requestPayload: '{"plate":"ABC123"}',
          responsePayload: '{"status":"ok"}',
          httpStatus: 200,
          durationMs: 42,
          loggedAt: "2026-06-11T10:00:00Z",
        },
      ],
      total: 1,
      page: 1,
      pageSize: 20,
    },
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  }),
}));

function renderWithProviders(ui: React.ReactElement) {
  const qc = new QueryClient();
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

describe("IntegrationLogsTable", () => {
  it("expands JSON payload when row is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<IntegrationLogsTable tenantId="00000000-0000-0000-0000-000000000099" />);

    expect(screen.getByText("mock")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /mock/i }));

    expect(screen.getByText(/"plate"/)).toBeInTheDocument();
    expect(screen.getByText(/"status"/)).toBeInTheDocument();
  });
});
