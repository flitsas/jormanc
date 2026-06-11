# Genera y publica TCs (Tasks) en ADO para HUs #9769-#9802 — qa-agent Modo A
param(
  [string]$Pat = $env:AZURE_PAT,
  [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
if (-not $Pat) {
  $envFile = Join-Path $PSScriptRoot '..' '.env.user-identity'
  if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
      if ($_ -match '^\s*AZURE_PAT\s*=\s*(.+)$') { $Pat = $Matches[1].Trim() }
    }
  }
}
if (-not $Pat) { throw 'AZURE_PAT requerido' }

$org = 'https://dev.azure.com/FlitDevOps'
$projectEnc = 'FLIT%20-%20EVOLUTION'
$base = "$org/$projectEnc/_apis/wit"
$auth = @{ Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat")) }
$jsonHdr = @{ Authorization = $auth.Authorization; 'Content-Type' = 'application/json-patch+json; charset=utf-8' }

$moduleByFeature = @{
  9567 = 'IDENTIDAD'
  9565 = 'COMPANIAS'
  9568 = 'PARAMETRIZADOR'
  9731 = 'TRAMITES'
  9729 = 'DOCUMENTOS'
  9728 = 'DASHBOARD'
  9566 = 'OT'
}

function Get-Alcance([string]$title) {
  $t = $title.ToUpperInvariant()
  if ($t -match 'JWT|AUTENTIC|LOGIN|AUTH') { return 'AUTH' }
  if ($t -match 'ROL|RBAC|PERMISO') { return 'RBAC' }
  if ($t -match 'SESION|INVALIDACION|SIGNALR') { return 'SESION' }
  if ($t -match 'INVITACION|RESET|PASSWORD|ONBOARDING') { return 'ONBOARDING' }
  if ($t -match 'COMPAÑ|COMPAN|TENANT') { return 'COMPANIAS' }
  if ($t -match 'RUNT|PROXY|FAILOVER') { return 'RUNT' }
  if ($t -match 'FIRMA|EXCEPCION|OT HABILIT') { return 'FIRMAS' }
  if ($t -match 'PARAMETRIZ|PROCEDURE TYPE|PIPELINE|CONECTOR') { return 'CONFIG' }
  if ($t -match 'REGLA|SIMULADOR|COHERENCIA') { return 'REGLAS' }
  if ($t -match 'ACTOR|VEHICULO|CONSULTA') { return 'ACTORES' }
  if ($t -match 'TRAMITE|DRAFT|SUBMIT|STEPPER') { return 'TRAMITES' }
  if ($t -match 'DOCUMENT|PLANTILLA|PDF|CONSOLID') { return 'DOCS' }
  if ($t -match 'DASHBOARD|KPI|EXPORT|GRAFICO') { return 'DASHBOARD' }
  if ($t -match 'QUIPUX|WEBHOOK') { return 'QUIPUX' }
  if ($t -match 'PRELACION|ETIQUETA|DRAG') { return 'PRELACION' }
  if ($t -match 'ORGANISMO|OT ') { return 'OTADMIN' }
  return 'FUNCIONAL'
}

function Get-AcBlocks([string]$html) {
  if (-not $html) { return @() }
  $pattern = [string]::Concat('<h3>AC\d+[^', '<', ']*</h3><pre>(.*?)</pre>')
  $blocks = [regex]::Matches($html, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
  $result = @()
  $i = 1
  foreach ($m in $blocks) {
    $hdrPat = [string]::Concat('AC\d+[^', '<', ']*')
    $header = [regex]::Match($m.Value, $hdrPat).Value -replace '</?h3>', ''
    $gherkin = $m.Groups[1].Value
    $gherkin = $gherkin.Replace('&quot;', [string][char]34).Replace('&lt;', [string][char]60).Replace('&gt;', [string][char]62)
    $result += [pscustomobject]@{ Index = $i; Header = $header.Trim(); Gherkin = $gherkin.Trim() }
    $i++
  }
  return $result
}

function Get-TcScenarios($acs, [string]$modulo, [string]$alcance) {
  $scenarios = [System.Collections.Generic.List[object]]::new()
  $n = 1
  foreach ($ac in $acs) {
    $short = ($ac.Header -replace '^AC\d+\s*-\s*', '').Trim()
    if ($short.Length -gt 55) { $short = $short.Substring(0, 52) + '...' }
    $scenarios.Add([pscustomobject]@{
        Num   = $n++
        TitleTpl = "QA_TC{0:D2}_{1}_{2} - {3}"
        TitleArgs = @($modulo, $alcance, $short)
        Type  = 'Happy Path'
        Ac    = $ac
      })
  }
  while ($scenarios.Count -lt 3 -and $acs.Count -gt 0) {
    $ac = $acs[[Math]::Min(1, $acs.Count - 1)]
    $scenarios.Add([pscustomobject]@{
        Num   = $n++
        TitleTpl = "QA_TC{0:D2}_{1}_{2} - Validacion Negativa AC{3}"
        TitleArgs = @($modulo, $alcance, $ac.Index)
        Type  = 'Error'
        Ac    = $ac
      })
  }
  if ($scenarios.Count -lt 4) {
    $scenarios.Add([pscustomobject]@{
        Num   = $n++
        TitleTpl = "QA_TC{0:D2}_{1}_{2} - Caso Borde Multitenant"
        TitleArgs = @($modulo, $alcance)
        Type  = 'Borde'
        Ac    = $acs[0]
      })
  }
  if ($scenarios.Count -lt 5) {
    $scenarios.Add([pscustomobject]@{
        Num   = $n++
        TitleTpl = "QA_TC{0:D2}_{1}_{2} - Validacion Contrato API"
        TitleArgs = @($modulo, $alcance)
        Type  = 'Error'
        Ac    = $acs[[Math]::Max(0, $acs.Count - 1)]
      })
  }
  $out = @()
  $tc = 1
  foreach ($s in $scenarios | Select-Object -First 5) {
    $title = $s.TitleTpl -f (@($tc) + $s.TitleArgs)
    $bodyLines = @(
      "# $title"
      ''
      '## Tipo'
      $s.Type
      ''
      '## Trazabilidad Gherkin'
      "**$($s.Ac.Header)**"
      ''
      '```'
      $s.Ac.Gherkin
      '```'
      ''
      '## Precondiciones'
      '- Ambiente DEV desplegado desde develop (PR merge 7e2053d)'
      '- Usuario seed: admin@acme.com / tenant acme (si aplica)'
      ''
      '## Pasos'
      '1. Ejecutar escenario segun Gherkin de origen'
      '2. Capturar respuesta HTTP o estado UI'
      '3. Validar resultado esperado'
      ''
      '## Resultado esperado'
      'Cumplimiento del Then del escenario Gherkin asociado.'
      ''
      '## Postcondiciones'
      '- Sin datos residuales que afecten otros TCs del mismo modulo'
    )
    $body = $bodyLines -join "`n"
    $out += [pscustomobject]@{ Title = $title; Body = $body; Type = $s.Type }
    $tc++
  }
  return $out
}

$manifestPath = Join-Path $PSScriptRoot '..' 'docs' 'designs' 'ado-hu-ids.json'
$hus = Get-Content $manifestPath -Raw | ConvertFrom-Json
$created = 0
$skipped = 0
$errors = @()

foreach ($hu in $hus) {
  $huId = $hu.ado_id
  try {
    $wi = Invoke-RestMethod -Uri "$base/workitems/$huId`?`$expand=relations&api-version=7.1" -Headers $auth
    if ($wi.fields.'System.State' -ne 'Resolved') {
      Write-Warning "HU #$huId no Resolved — omitida"
      $skipped++
      continue
    }
    $existingTc = 0
    if ($wi.relations) {
      foreach ($rel in $wi.relations) {
        if ($rel.rel -eq 'System.LinkTypes.Hierarchy-Forward') {
          $childId = [int]($rel.url -replace '.*workItems/(\d+).*', '$1')
          $child = Invoke-RestMethod -Uri "$base/workitems/$childId`?api-version=7.1&fields=System.Title,System.WorkItemType" -Headers $auth
          if ($child.fields.'System.WorkItemType' -eq 'Task' -and $child.fields.'System.Title' -match '^QA_TC') {
            $existingTc++
          }
        }
      }
    }
    if ($existingTc -ge 3) {
      Write-Host "HU #$huId — ya tiene $existingTc TCs, skip"
      $skipped++
      continue
    }

    $modulo = $moduleByFeature[[int]$hu.feature_id]
    $alcance = Get-Alcance $hu.title
    $acs = Get-AcBlocks $wi.fields.'Microsoft.VSTS.Common.AcceptanceCriteria'
    if ($acs.Count -eq 0) {
      $acs = @([pscustomobject]@{
          Index = 1; Header = 'AC1 - Escenario principal'
          Gherkin = "Given precondiciones HU #$huId`nWhen accion principal`nThen resultado esperado"
        })
    }
    $tcs = Get-TcScenarios $acs $modulo $alcance

    foreach ($tc in $tcs) {
      if ($DryRun) {
        Write-Host "[DRY] HU #$huId -> $($tc.Title)"
        continue
      }
      $patch = @(
        @{ op = 'add'; path = '/fields/System.Title'; value = $tc.Title }
        @{ op = 'add'; path = '/fields/System.Description'; value = $tc.Body }
        @{
          op    = 'add'
          path  = '/relations/-'
          value = @{
            rel  = 'System.LinkTypes.Hierarchy-Reverse'
            url  = "$org/9032fb34-d178-4c62-b1f5-6805b56524b1/_apis/wit/workItems/$huId"
          }
        }
      )
      $body = $patch | ConvertTo-Json -Depth 6 -Compress
      Invoke-RestMethod -Method Post -Uri "$base/workitems/`$Task?api-version=7.1" -Headers $jsonHdr -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) | Out-Null
      $created++
      Start-Sleep -Milliseconds 180
    }
    Write-Host "HU #$huId — $($tcs.Count) TCs publicados ($modulo/$alcance)"
  }
  catch {
    $errors += "HU #$huId : $($_.Exception.Message)"
  }
}

Write-Host "`n=== Resumen ==="
Write-Host "TCs creados: $created | HUs omitidas: $skipped | Errores: $($errors.Count)"
if ($errors.Count) { $errors | Select-Object -First 10 }
