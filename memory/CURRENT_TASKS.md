# Current Tasks

**Last Updated:** 2026-06-10

## Active Work

### 1. Documentation Reconciliation

- [x] Update core docs and memory files to match the codebase as it exists today
- [ ] Continue tightening docs where planned versus implemented surfaces could still be confused

### 2. CTI-to-CoroMES Connector

- [x] Capture initial Welch CTI/EPS integration facts and public CTI/ePS product research in `docs/CTI_RESEARCH_BRIEF.md`
- [x] Create reusable Codex skill `$welch-cti-connector`
- [x] Back up `$welch-cti-connector` in the repo under `docs/skills/welch-cti-connector`
- [x] Start the .NET CTI connector framework in `integration/CoroMES.Integration.Cti`
- [ ] Gather CTI data source details, access method, schema/files/API, refresh cadence, and ownership boundaries
- [ ] Collect representative `.dat` and `.cov` samples
- [ ] Register real CTI file layouts in the parser once samples are available
- [ ] Define ingestion, mapping, reconciliation, sync status, and error handling flows
- [ ] Add a runnable host or background workflow for CTI ingestion

### 3. i3X and Translator Refinement

- [x] Add i3X client and repository adapter support
- [x] Investigate `soeprbp/mes-vision` as the first i3X/vision telemetry test endpoint
- [x] Refresh i3X standards baseline against CESMII `1.0` branch and `1.0.0` tag
- [ ] Refine translator type mappings where placeholder reuse still exists
- [ ] Decide whether i3X is a primary runtime mode, a migration bridge, or both
- [ ] Update CoroMES i3X client for CESMII 1.0 route shapes, subscription client scoping, sync batches, and bulk writes
- [ ] Add MES-Vision compatibility coverage after the core i3X client is aligned to CESMII 1.0
- [ ] Align translator outputs with future MCP exposure requirements

### 4. Blazor Host Migration

- [x] Establish `CoroMES.Web` as the forward Blazor host for admin and shop-floor display workflows
- [x] Preserve the pre-Blazor source state at branch/tag `pre-blazor-2026-06-10`
- [x] Record backup zip path: `C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES-source-backup-2026-06-10.zip`
- [x] Add first Blazor admin/API auth gate from Jane security pass
- [x] Add first-pass audit logging for equipment create/update/delete
- [x] Add integration coverage for Blazor auth gates and equipment audit logging
- [x] Move API endpoint composition out of `Program.cs` into Blazor host endpoint modules
- [x] Persist display builder configurations and load them through the Blazor viewer
- [x] Expand audit logging to display definitions and UpKeep sync/downtime placeholders
- [x] Add admin settings scaffold for integration endpoints and feature flags
- [ ] Replace static admin pages with Blazor routes while preserving `/admin` compatibility
- [ ] Replace static display viewer and builder pages with Blazor routes while preserving `/displays/viewer.html` and `/displays/builder.html` redirects
- [ ] Keep Blazor screens aligned with current API route parity in `docs/API.md`
- [ ] Move Upkeep placeholder asset matching behind a dedicated integration boundary

### 5. Product Surface Expansion

- [ ] Decide whether to implement the documented reporting API or reduce its documented scope
- [ ] Decide whether to implement the documented industrial API or reduce its documented scope
- [ ] Add MES-Vision source registry, telemetry readings, and event history storage
- [ ] Add admin mapping for MES-Vision cameras/zones to CoroMES equipment
- [ ] Add dashboard/reporting widgets for vision-derived runtime, idle time, camera health, and zone activity
- [ ] Replace placeholder Upkeep behavior with a real integration strategy
- [ ] Replace temporary admin access-code gate with the chosen enterprise identity model
- [ ] Expand audit logging beyond equipment, display, and UpKeep placeholder mutations
- [ ] Expand inventory, workforce, and quality write workflows

### 6. Testing

- [x] Unit test CTI ingestion components
- [x] Unit test i3X client behavior
- [ ] Add API integration tests
- [x] Keep Blazor auth and audit integration tests running as the migration smoke suite
- [ ] Add persistence-mode coverage for SQLite and PostgreSQL
- [ ] Add CTI end-to-end ingestion tests with representative sample files

## Blockers

### External Information Still Needed

- CTI01 / `svwpcti01` service inventory
- Amtech task server job inventory
- staging share folder inventory
- representative CTI sample files for normal and exception scenarios

## Next Actions

1. Collect CTI source-system details and sample files
2. Turn the CTI framework into a runnable connector host
3. Keep the Blazor auth/audit/display integration tests green
4. Align the CoroMES i3X client to CESMII 1.0 before building the MES-Vision collector
5. Move UpKeep placeholder asset matching behind a dedicated mock/live integration boundary
6. Persist admin integration settings and feature flags with secret-safe storage
7. Build the first read-only MES-Vision collector using i3X discovery, value polling, and history polling
8. Start either implementing or pruning the reporting and industrial surfaces promised by older docs
