# Creates User Stories under Feature 8910 - run from repo root
$ErrorActionPreference = "Stop"
$org = "https://dev.azure.com/FlitDevOps"
$project = "FLIT - EVOLUTION"
$parentId = 8910
$assignee = "hector.rivera@flitsas.com"
$traceHtml = '<div>🤖 Acción registrada por @Cursor Agent (Auto) usando el skill <b>@skill-crear-hu</b> bajo la supervisión de <a href="mailto:hector.rivera@flitsas.com">@Hector Fabio Rivera Heurfano</a></div>'

$stories = @(
  @{ Title = "FRONTEND - Renombre navegación y CTAs principales del módulo Trámites"; File = "hu-01-navegacion-ctas.md"; SP = 2 },
  @{ Title = "FRONTEND - Alinear títulos del wizard y dashboard (H1, continuar trámite)"; File = "hu-02-wizard-dashboard.md"; SP = 3 },
  @{ Title = "FRONTEND - Etiquetas en español para tipos de procedimiento (matrícula inicial, traspaso)"; File = "hu-03-tipos-procedimiento.md"; SP = 3 },
  @{ Title = "FRONTEND - Actualizar pruebas E2E y unitarias por renombre UI Trámites"; File = "hu-04-tests-e2e.md"; SP = 3 }
)

$created = @()
foreach ($s in $stories) {
  $path = Join-Path $PSScriptRoot $s.File
  $json = az boards work-item create --type "User Story" --title $s.Title --description "@$path" --assigned-to $assignee --org $org --project $project -o json | ConvertFrom-Json
  $id = $json.id
  az boards work-item relation add --id $id --relation-type parent --target-id $parentId --org $org -o none | Out-Null
  az boards work-item update --id $id --fields "Microsoft.VSTS.Scheduling.StoryPoints=$($s.SP)" --org $org -o none | Out-Null
  az boards work-item update --id $id --discussion $traceHtml --org $org -o none | Out-Null
  $created += [PSCustomObject]@{ Id = $id; Title = $s.Title; SP = $s.SP; Url = "https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/$id" }
}
$created | ConvertTo-Json -Depth 3
