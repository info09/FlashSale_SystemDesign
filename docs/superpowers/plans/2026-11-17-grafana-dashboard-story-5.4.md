# Grafana Dashboard (Story 5.4) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provision Grafana datasource and dashboard so DevOps can view FlashSale metrics (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining) scraped by Prometheus from API and Worker.

**Architecture:** Add Grafana file provisioning under `monitoring/grafana/` (Prometheus datasource + dashboard provider + dashboard JSON) and mount it into `flashsale-grafana` in `docker-compose.yml`. Reuse existing Prometheus scrape jobs (`flashsale-api`, `flashsale-worker`) and existing metric names exported as `flash_sale_<lowercased_name>` gauges.

**Tech Stack:** Grafana provisioning (YAML/JSON), Prometheus datasource, Docker Compose volume mounts.

**Spec:** `docs/status.md` (Story 5.4, Phase 5 FR-06/NFR-07), `docs/architecture.md`, `docs/prd.md`, `docs/decisions.md`. Approved implementation direction in conversation: provision datasource + overview dashboard with 5 metrics split by `job`, mount in compose; do not tick 5.4 complete without required tests per `docs/status.md` rules (5.7 covers dashboard e2e separately).

## Global Constraints

- Follow `docs/domain-glossary.md` terminology in titles/labels where applicable (Metric, Prometheus, Grafana Dashboard, QueueDepth, DlqSize, InventoryRemaining, P99Latency, ErrorRate).
- Do not hardcode secrets; Grafana admin credentials remain as currently configured in `docker-compose.yml` unless explicitly changed.
- Prometheus target in compose network: `http://flashsale-prometheus:9090` (service name `flashsale-prometheus`).
- Metrics endpoint/path already configured: `/api/v1/metrics`; metric names are `flash_sale_p99latency`, `flash_sale_errorrate`, `flash_sale_queuedepth`, `flash_sale_dlqsize`, `flash_sale_inventoryremaining`.
- Story 5.4 checkbox in `docs/status.md` may only be marked `[x]` when unit + integration + e2e tests related to 5.4 pass; otherwise record progress without ticking.

## Review Focus

- Datasource URL/name mismatch: dashboard must query Prometheus datasource UID/name provisioned (expect `Prometheus`, uid `prometheus`) so panels do not show “No data source”.
- Wrong metric/query names: PromQL must use `flash_sale_p99latency`, `flash_sale_errorrate`, `flash_sale_queuedepth`, `flash_sale_dlqsize`, `flash_sale_inventoryremaining` and label `job` values `flashsale-api`, `flashsale-worker`.
- Provisioning not loaded: compose mounts/paths must match Grafana provisioning locations (`/etc/grafana/provisioning/...`, `/var/lib/grafana/dashboards`).
- Dashboard JSON invalid: malformed JSON breaks provisioning; validate syntax.
- Status log accuracy: do not falsely tick 5.4 complete if test evidence required by repo rules is not produced.

---

### Task 1: Provision Grafana datasource and dashboard provider

**Files:**
- Create: `monitoring/grafana/provisioning/datasources/datasource.yml`
- Create: `monitoring/grafana/provisioning/dashboards/dashboards.yml`

**Interfaces:**
- Consumes: existing Prometheus service `flashsale-prometheus:9090` (`monitoring/prometheus.yml`, `docker-compose.yml`)
- Produces: Grafana datasource `Prometheus` (uid `prometheus`, default) and dashboard provider loading JSON files from `/var/lib/grafana/dashboards`

- [ ] **Step 1: Create datasource provisioning file**

Create `monitoring/grafana/provisioning/datasources/datasource.yml` with:
```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    uid: prometheus
    type: prometheus
    access: proxy
    url: http://flashsale-prometheus:9090
    isDefault: true
    editable: false
```

- [ ] **Step 2: Create dashboard provider provisioning file**

Create `monitoring/grafana/provisioning/dashboards/dashboards.yml` with:
```yaml
apiVersion: 1

providers:
  - name: "FlashSale Dashboards"
    orgId: 1
    folder: "FlashSale"
    type: file
    disableDeletion: false
    editable: true
    options:
      path: /var/lib/grafana/dashboards
```

- [ ] **Step 3: Verify YAML files parse**

Run: `powershell -NoProfile -Command "Get-ChildItem monitoring/grafana/provisioning -Recurse -Filter *.yml | ForEach-Object { Write-Host $_.FullName }"`
Expected: both YAML files listed. (Optional deeper parse if Python/yq available; otherwise proceed to Task 2 and rely on JSON validation + compose review.)

- [ ] **Step 4: Commit (if committing)**

Not requested by user (no auto commit).

### Task 2: Create FlashSale overview dashboard JSON

**Files:**
- Create: `monitoring/grafana/dashboards/flashsale-overview.json`
- Test/Verify: JSON syntax validation command

**Interfaces:**
- Consumes: datasource uid `prometheus`; Prometheus metrics `flash_sale_p99latency`, `flash_sale_errorrate`, `flash_sale_queuedepth`, `flash_sale_dlqsize`, `flash_sale_inventoryremaining` with label `job`
- Produces: dashboard titled `FlashSale Overview` showing all 5 metrics for jobs `flashsale-api` and `flashsale-worker`

- [ ] **Step 1: Author dashboard JSON**

Create `monitoring/grafana/dashboards/flashsale-overview.json` containing:
- Dashboard metadata: `title` = `FlashSale Overview`, `uid` = `flashsale-overview`, `timezone` = `browser`, `schemaVersion` (current stable, e.g. 39), `refresh` = `10s`.
- Templating not required.
- Panels (time series/stat as appropriate) covering:
  - `P99Latency` (`flash_sale_p99latency{job=~"flashsale-api|flashsale-worker"}`) by `job`, unit ms.
  - `ErrorRate` (`flash_sale_errorrate{job=~"flashsale-api|flashsale-worker"}`) by `job`, unit percent.
  - `QueueDepth` (`flash_sale_queuedepth{job=~"flashsale-api|flashsale-worker"}`) by `job`.
  - `DlqSize` (`flash_sale_dlqsize{job=~"flashsale-api|flashsale-worker"}`) by `job`.
  - `InventoryRemaining` (`flash_sale_inventoryremaining{job=~"flashsale-api|flashsale-worker"}`) by `job`.
- Each panel datasource: `{ "type": "prometheus", "uid": "prometheus" }`.

- [ ] **Step 2: Validate JSON syntax**

Run: `powershell -NoProfile -Command "Get-Content monitoring/grafana/dashboards/flashsale-overview.json -Raw | ConvertFrom-Json | Out-Null; Write-Host 'JSON_OK'"`
Expected: `JSON_OK`

- [ ] **Step 3: Spot-check metric names/queries in file**

Run: `powershell -NoProfile -Command "Select-String -Path monitoring/grafana/dashboards/flashsale-overview.json -Pattern 'flash_sale_p99latency|flash_sale_errorrate|flash_sale_queuedepth|flash_sale_dlqsize|flash_sale_inventoryremaining|flashsale-api|flashsale-worker|\"uid\": \"prometheus\"' | Measure-Object | Select-Object -ExpandProperty Count"`
Expected: count > 0 (all key tokens present).

- [ ] **Step 4: Commit (if committing)**

Not requested by user.

### Task 3: Mount provisioning in Docker Compose and update status docs

**Files:**
- Modify: `docker-compose.yml` (service `flashsale-grafana` volumes)
- Modify: `docs/status.md` (Story 5.4 progress/current status)

**Interfaces:**
- Consumes: files created in Tasks 1-2
- Produces: Grafana container mounts provisioning and dashboards; status log reflects 5.4 implementation progress accurately

- [ ] **Step 1: Update Grafana service volumes**

In `docker-compose.yml` under `flashsale-grafana.volumes`, keep `flashsale-grafana-data:/var/lib/grafana` and add:
```yaml
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning:ro
      - ./monitoring/grafana/dashboards:/var/lib/grafana/dashboards:ro
```

- [ ] **Step 2: Verify compose file still well-formed (best effort)**

Run: `powershell -NoProfile -Command "Select-String -Path docker-compose.yml -Pattern 'grafana/provisioning|grafana/dashboards'"`
Expected: both mount lines present. (Full `docker compose config` depends on `.env`; run if environment available and report result.)

- [ ] **Step 3: Update `docs/status.md`**

- Update Phase 5/Story 5.4 line to reflect implementation done but not ticked if tests not executed for 5.4 per repo rule, e.g. keep `- [] Story 5.4 ...` and append note in description or record under section 3; **do not mark `[x]`** without unit+integration+e2e evidence for 5.4.
- Update “Trạng thái hiện tại” (phase/story/lần cập nhật gần nhất) to mention Story 5.4 infra provisioning implemented (files added, compose mounted), pending 5.4 test completion/tick and 5.7 dashboard e2e.

- [ ] **Step 4: Final verification summary**

Run targeted checks:
- `powershell -NoProfile -Command "Test-Path monitoring/grafana/provisioning/datasources/datasource.yml,monitoring/grafana/provisioning/dashboards/dashboards.yml,monitoring/grafana/dashboards/flashsale-overview.json"`
Expected: all `True`.
Report to user: files changed, verification results, that Story 5.4 not ticked due to test-rule/5.7 dependency unless user directs otherwise.

- [ ] **Step 5: Commit (if committing)**

Not requested by user.
