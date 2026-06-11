#!/usr/bin/env python3
"""Publica TCs (Tasks) en ADO para HUs #9769-#9802 — qa-agent Modo A."""
from __future__ import annotations

import base64
import json
import os
import re
import sys
import time
import urllib.request
from pathlib import Path

ORG = "https://dev.azure.com/FlitDevOps"
PROJECT_ENC = "FLIT%20-%20EVOLUTION"
BASE = f"{ORG}/{PROJECT_ENC}/_apis/wit"
PROJECT_ID = "9032fb34-d178-4c62-b1f5-6805b56524b1"

MODULE_BY_FEATURE = {
    9567: "IDENTIDAD",
    9565: "COMPANIAS",
    9568: "PARAMETRIZADOR",
    9731: "TRAMITES",
    9729: "DOCUMENTOS",
    9728: "DASHBOARD",
    9566: "OT",
}


def load_pat() -> str:
    pat = os.environ.get("AZURE_PAT")
    if pat:
        return pat
    env_file = Path(__file__).resolve().parent.parent / ".env.user-identity"
    if env_file.exists():
        for line in env_file.read_text(encoding="utf-8").splitlines():
            if line.strip().startswith("AZURE_PAT="):
                return line.split("=", 1)[1].strip()
    raise SystemExit("AZURE_PAT required")


def api(method: str, url: str, pat: str, body: list | None = None) -> dict:
    data = json.dumps(body, ensure_ascii=False).encode("utf-8") if body is not None else None
    req = urllib.request.Request(
        url,
        data=data,
        method=method,
        headers={
            "Authorization": "Basic " + base64.b64encode(f":{pat}".encode()).decode(),
            "Content-Type": "application/json-patch+json; charset=utf-8",
        },
    )
    with urllib.request.urlopen(req, timeout=60) as resp:
        raw = resp.read().decode("utf-8")
        return json.loads(raw) if raw else {}


def get_alcance(title: str) -> str:
    t = title.upper()
    rules = [
        (r"JWT|AUTENTIC|LOGIN|AUTH", "AUTH"),
        (r"ROL|RBAC|PERMISO", "RBAC"),
        (r"SESION|INVALIDACION|SIGNALR", "SESION"),
        (r"INVITACION|RESET|PASSWORD|ONBOARDING", "ONBOARDING"),
        (r"COMPAÑ|COMPAN|TENANT", "COMPANIAS"),
        (r"RUNT|PROXY|FAILOVER", "RUNT"),
        (r"FIRMA|EXCEPCION|OT HABILIT", "FIRMAS"),
        (r"PARAMETRIZ|PROCEDURE TYPE|PIPELINE|CONECTOR", "CONFIG"),
        (r"REGLA|SIMULADOR|COHERENCIA", "REGLAS"),
        (r"ACTOR|VEHICULO|CONSULTA", "ACTORES"),
        (r"TRAMITE|DRAFT|SUBMIT|STEPPER", "TRAMITES"),
        (r"DOCUMENT|PLANTILLA|PDF|CONSOLID", "DOCS"),
        (r"DASHBOARD|KPI|EXPORT|GRAFICO", "DASHBOARD"),
        (r"QUIPUX|WEBHOOK", "QUIPUX"),
        (r"PRELACION|ETIQUETA|DRAG", "PRELACION"),
        (r"ORGANISMO|\bOT\b", "OTADMIN"),
    ]
    for pattern, alcance in rules:
        if re.search(pattern, t):
            return alcance
    return "FUNCIONAL"


def parse_ac_blocks(html: str) -> list[dict]:
    if not html:
        return []
    blocks = re.findall(r"<h3>(AC\d+[^<]*)</h3><pre>(.*?)</pre>", html, re.DOTALL)
    result = []
    for i, (header, gherkin) in enumerate(blocks, start=1):
        gherkin = (
            gherkin.replace("&quot;", '"')
            .replace("&lt;", "<")
            .replace("&gt;", ">")
            .strip()
        )
        result.append({"index": i, "header": header.strip(), "gherkin": gherkin})
    return result


def build_tcs(acs: list[dict], modulo: str, alcance: str) -> list[dict]:
    scenarios: list[dict] = []
    for ac in acs:
        short = re.sub(r"^AC\d+\s*[-–—]\s*", "", ac["header"]).strip()
        if len(short) > 55:
            short = short[:52] + "..."
        scenarios.append({"tpl": "{tc:02d}", "args": (modulo, alcance, short), "type": "Happy Path", "ac": ac})

    while len(scenarios) < 3 and acs:
        ac = acs[min(1, len(acs) - 1)]
        scenarios.append(
            {
                "tpl": f"Validacion Negativa AC{ac['index']}",
                "args": (modulo, alcance),
                "type": "Error",
                "ac": ac,
            }
        )
    if len(scenarios) < 4 and acs:
        scenarios.append(
            {"tpl": "Caso Borde Multitenant", "args": (modulo, alcance), "type": "Borde", "ac": acs[0]}
        )
    if len(scenarios) < 5 and acs:
        scenarios.append(
            {
                "tpl": "Validacion Contrato API",
                "args": (modulo, alcance),
                "type": "Error",
                "ac": acs[-1],
            }
        )

    out = []
    for i, s in enumerate(scenarios[:5], start=1):
        if s["tpl"] == "{tc:02d}":
            title = f"QA_TC{i:02d}_{s['args'][0]}_{s['args'][1]} - {s['args'][2]}"
        else:
            title = f"QA_TC{i:02d}_{s['args'][0]}_{s['args'][1]} - {s['tpl']}"
        body = "\n".join(
            [
                f"# {title}",
                "",
                "## Tipo",
                s["type"],
                "",
                "## Trazabilidad Gherkin",
                f"**{s['ac']['header']}**",
                "",
                "```",
                s["ac"]["gherkin"],
                "```",
                "",
                "## Precondiciones",
                "- Ambiente DEV desplegado desde develop (merge 7e2053d)",
                "- Usuario seed: admin@acme.com / tenant acme (si aplica)",
                "",
                "## Pasos",
                "1. Ejecutar escenario segun Gherkin de origen",
                "2. Capturar respuesta HTTP o estado UI",
                "3. Validar resultado esperado",
                "",
                "## Resultado esperado",
                "Cumplimiento del Then del escenario Gherkin asociado.",
                "",
                "## Postcondiciones",
                "- Sin datos residuales que afecten otros TCs del mismo modulo",
            ]
        )
        out.append({"title": title, "body": body, "type": s["type"]})
    return out


def count_existing_tcs(wi: dict, pat: str) -> int:
    count = 0
    for rel in wi.get("relations") or []:
        if rel.get("rel") != "System.LinkTypes.Hierarchy-Forward":
            continue
        child_id = int(rel["url"].rstrip("/").split("/")[-1])
        child = api("GET", f"{BASE}/workitems/{child_id}?api-version=7.1&fields=System.Title,System.WorkItemType", pat)
        fields = child.get("fields", {})
        if fields.get("System.WorkItemType") == "Task" and str(fields.get("System.Title", "")).startswith("QA_TC"):
            count += 1
    return count


def main() -> int:
    pat = load_pat()
    print("ADO PAT loaded, starting TC publish...", flush=True)
    manifest = json.loads(
        (Path(__file__).resolve().parent.parent / "docs" / "designs" / "ado-hu-ids.json").read_text(encoding="utf-8")
    )
    only_hu = int(sys.argv[1]) if len(sys.argv) > 1 else None
    if only_hu:
        manifest = [h for h in manifest if h["ado_id"] == only_hu]
    created = skipped = 0
    errors: list[str] = []

    for hu in manifest:
        hu_id = hu["ado_id"]
        try:
            wi = api("GET", f"{BASE}/workitems/{hu_id}?$expand=relations&api-version=7.1", pat)
            if wi.get("fields", {}).get("System.State") != "Resolved":
                skipped += 1
                continue
            if count_existing_tcs(wi, pat) >= 3:
                print(f"HU #{hu_id} — skip (TCs existentes)")
                skipped += 1
                continue

            modulo = MODULE_BY_FEATURE[int(hu["feature_id"])]
            alcance = get_alcance(hu["title"])
            acs = parse_ac_blocks(wi.get("fields", {}).get("Microsoft.VSTS.Common.AcceptanceCriteria", ""))
            if not acs:
                acs = [
                    {
                        "index": 1,
                        "header": "AC1 - Escenario principal",
                        "gherkin": f"Given precondiciones HU #{hu_id}\nWhen accion principal\nThen resultado esperado",
                    }
                ]
            tcs = build_tcs(acs, modulo, alcance)

            for tc in tcs:
                patch = [
                    {"op": "add", "path": "/fields/System.Title", "value": tc["title"]},
                    {"op": "add", "path": "/fields/System.Description", "value": tc["body"]},
                    {
                        "op": "add",
                        "path": "/relations/-",
                        "value": {
                            "rel": "System.LinkTypes.Hierarchy-Reverse",
                            "url": f"{ORG}/{PROJECT_ID}/_apis/wit/workItems/{hu_id}",
                        },
                    },
                ]
                api("POST", f"{BASE}/workitems/$Task?api-version=7.1", pat, patch)
                created += 1
                time.sleep(0.15)

            print(f"HU #{hu_id} — {len(tcs)} TCs ({modulo}/{alcance})")
        except Exception as exc:  # noqa: BLE001
            errors.append(f"HU #{hu_id}: {exc}")

    print(f"\n=== Resumen: creados={created} omitidas={skipped} errores={len(errors)} ===")
    for e in errors[:10]:
        print(e)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
