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
- [ ] Refine translator type mappings where placeholder reuse still exists
- [ ] Decide whether i3X is a primary runtime mode, a migration bridge, or both
- [ ] Align translator outputs with future MCP exposure requirements

### 4. Application and UI Direction

- [ ] Decide whether the primary UI migration path is Blazor
- [ ] If yes, scaffold a `CoroMES.Web` front end and begin replacing static admin pages
- [ ] Move API business logic out of `Program.cs` into application services or endpoint modules

### 5. Product Surface Expansion

- [ ] Decide whether to implement the documented reporting API or reduce its documented scope
- [ ] Decide whether to implement the documented industrial API or reduce its documented scope
- [ ] Replace placeholder Upkeep behavior with a real integration strategy
- [ ] Expand inventory, workforce, and quality write workflows

### 6. Testing

- [x] Unit test CTI ingestion components
- [x] Unit test i3X client behavior
- [ ] Add API integration tests
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
3. Choose the UI direction, with Blazor currently the preferred target if we move to a .NET front end
4. Refactor API composition so the backend is easier to extend safely
5. Start either implementing or pruning the reporting and industrial surfaces promised by older docs
