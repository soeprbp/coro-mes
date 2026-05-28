---
name: welch-cti-connector
description: "Use when working on Welch Packaging's CTI/EPS portion of CoroMES: researching Corrugated Technologies Inc. / eProductivity Software, designing or implementing the CTI-to-CoroMES connector, discovering CTI/Amtech services and file drops, parsing .dat/.cov staging files, mapping CTI/Amtech data into CoroMES/i3X models, or planning safe read-only extraction, validation, reconciliation, and cutover for the legacy CTI/Amtech ecosystem."
---

# Welch CTI Connector

Use this skill for the CTI/EPS portion of CoroMES. Treat CTI as Corrugated Technologies Inc. / eProductivity Software, not telephony.

## First Moves

1. If inside the CoroMES repo, read `docs/CTI_RESEARCH_BRIEF.md` before making CTI design or code changes.
2. Check `memory/DECISIONS.md` ADR-011 before choosing project structure or coupling points.
3. Separate confirmed Welch facts, public product research, assumptions needing verification, and general best practices.
4. Prefer implementation that is read-only toward CTI/Amtech production and idempotent toward CoroMES.

## Required Context

Load `references/cti-baseline.md` when the task involves architecture, discovery, source-system naming, mapping, parser design, reconciliation, or operational runbooks.

Use the repo research brief as the canonical current project artifact when available:

```text
docs/CTI_RESEARCH_BRIEF.md
```

## Integration Rules

- Do not propose or implement direct writes into CTI or Amtech production databases.
- Start with file-based extraction when possible because it mirrors Welch's legacy CTI/Amtech boundary.
- Add read-only database extraction only when files are incomplete, delayed, or lossy.
- Preserve raw source payloads before parsing.
- Use correlation IDs, source file names, file hashes, row numbers, source timestamps, and source-system IDs for traceability.
- Quarantine malformed or unknown files instead of deleting or mutating them.
- Treat missing records as a reconciliation problem, not as implicit deletes.
- Require explicit source evidence for canceled, voided, deleted, or reversed business states.
- Run in parallel and reconcile before cutover.

## Discovery Workflow

1. Inventory CTI01 / svwpcti01 services, especially CTI System Bridge and CTI Service Manager.
2. Inventory Amtech task server jobs and services, especially CorrExpv2 and AmtSRVProc.exe / Amtech Service Processor tasks.
3. Locate Welch-managed staging shares and archive/error/processed folders.
4. Capture file listings with names, sizes, extensions, timestamps, and retention behavior.
5. Collect sample file sets across normal, exception/rework, and canceled/voided scenarios.
6. Build at least two lineage maps:
   - Amtech job/order export to CTI floor schedule.
   - CTI production reporting back to Amtech and CoroMES.

## Implementation Shape

Prefer a separate CoroMES connector project for CTI ingestion, mapping, reconciliation, and sync status. Keep CTI-specific table names, file layouts, service names, and proprietary details out of core CoroMES modules.

Recommended pipeline:

```text
extract -> immutable raw stage -> parse/validate -> canonical map -> idempotent load -> reconcile
```

For parser or sync code, include:

- layout registry for discovered file formats
- schema validation
- retry policy for transient I/O failures
- quarantine/dead-letter folder
- structured logging
- batch watermarks
- reconciliation summaries

## Validation Expectations

Before calling CTI work complete, verify:

- record counts by domain/day
- production quantities and downtime totals
- file completeness against expected schedule
- payload hashes/checksums for critical records
- golden transaction trace from schedule to production to posting
- clear residual assumptions and next discovery artifacts needed
