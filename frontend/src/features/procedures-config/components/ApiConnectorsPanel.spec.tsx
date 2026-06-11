import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ApiConnectorsPanel } from "./ApiConnectorsPanel.js";
import type { ApiConnector } from "../api/procedures-config.schemas.js";

const CONNECTOR: ApiConnector = {
  id: "00000000-0000-0000-0000-000000000030",
  procedureTypeId: "00000000-0000-0000-0000-000000000099",
  tenantId: "00000000-0000-0000-0000-000000000088",
  name: "Consulta RUNT",
  endpoint: "/runt/vehicle",
  httpVerb: "GET",
  stepOrder: 2,
  paramBindings: {},
  responseMappings: {},
  isActive: true,
  createdAt: "2026-01-01T00:00:00Z",
};

describe("ApiConnectorsPanel", () => {
  it("AC3 lista conectores con nombre, endpoint, verbo y paso", () => {
    render(<ApiConnectorsPanel connectors={[CONNECTOR]} onSaveBindings={vi.fn()} />);
    expect(screen.getByText("Consulta RUNT")).toBeInTheDocument();
    expect(screen.getByText("/runt/vehicle")).toBeInTheDocument();
    expect(screen.getByText("GET")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
  });

  it("AC3 guarda param_bindings editados", async () => {
    const user = userEvent.setup();
    const onSave = vi.fn().mockResolvedValue(undefined);
    render(<ApiConnectorsPanel connectors={[CONNECTOR]} onSaveBindings={onSave} />);

    await user.click(screen.getByRole("button", { name: /editar bindings/i }));
    await user.type(screen.getByLabelText(/binding clave 1/i), "placa");
    await user.type(screen.getByLabelText(/binding valor 1/i), "step_1.field_placa");
    await user.click(screen.getByRole("button", { name: /^guardar$/i }));

    expect(onSave).toHaveBeenCalledWith(CONNECTOR.id, {
      placa: "step_1.field_placa",
    });
  });
});
