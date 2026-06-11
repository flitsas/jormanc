import { describe, expect, it } from "vitest";
import { detectTemplateMarkers } from "./templateMarkers.js";

describe("detectTemplateMarkers", () => {
  it("extrae marcadores únicos ordenados", () => {
    const html = "{{vehicle.plate}} y {{actor[vendedor].full_name}} y {{vehicle.plate}}";
    expect(detectTemplateMarkers(html)).toEqual([
      "actor[vendedor].full_name",
      "vehicle.plate",
    ]);
  });

  it("retorna arreglo vacío sin marcadores", () => {
    expect(detectTemplateMarkers("<p>Sin datos</p>")).toEqual([]);
  });
});
