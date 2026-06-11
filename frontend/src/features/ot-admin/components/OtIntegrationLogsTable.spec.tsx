import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { OtIntegrationLogsTable } from "./OtIntegrationLogsTable.js";

vi.mock("../api/ot-admin.api.js", () => ({
  useOtIntegrationLogs: () => ({
    data: {
      data: [
        {
          id: "00000000-0000-0000-0000-000000000001",
          otId: "00000000-0000-0000-0000-000000000010",
          tenantId: "00000000-0000-0000-0000-000000000099",
          eventType: "status_changed",
          procedureRef: "TRASP-02_EVE-8841",
          requestPayload: '{"event":"status_changed","new_status":"approved"}',
          responsePayload: '{"ok":true}',
          httpStatus: 200,
          durationMs: 120,
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

describe("OtIntegrationLogsTable", () => {
  it("expands JSON payload when row is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<OtIntegrationLogsTable otId="00000000-0000-0000-0000-000000000010" />);

    expect(screen.getByText("status_changed")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /status_changed/i }));

    expect(screen.getByText(/"new_status"/)).toBeInTheDocument();
    expect(screen.getByText(/"ok"/)).toBeInTheDocument();
  });
});
