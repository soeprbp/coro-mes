# CTI / EPS Research Brief for CoroMES Agents

Last researched: 2026-05-28

This brief is for coding agents and engineers working on the CoroMES CTI connector. It combines Welch-provided context with public research about Corrugated Technologies Inc. (CTI), eProductivity Software (ePS/EPS), Amtech Encore, and related corrugated MES/ERP product names.

## Agent Mission

Build the CTI-to-CoroMES integration as a low-risk, traceable migration connector. Prefer read-only extraction, immutable raw staging, field-level reconciliation, and parallel-run validation before cutover.

The connector must not write directly into CTI or Amtech production databases.

## Confirmed Welch Context

- At Welch, CTI means Corrugated Technologies Inc. / EPS, a legacy corrugated MES, not telephony.
- CTI is used for Elkhart plant-floor and scheduling workflows.
- CTI integrates with Amtech Encore.
- CTI System Bridge runs on CTI01 / svwpcti01 and is the CTI-Amtech import/export bridge.
- CTI Service Manager on CTI01 is used primarily for clamp-truck workflows.
- CorrExpv2 on the Amtech task server is historically the side that stops and may need restart during bridge incidents.
- Amtech Encore uses Amtech Service Processor tasks, including AmtSRVProc.exe, plus Welch-managed file shares.
- Welch does not use a local TrueCommerce Integration Service/agent for EDI file movement; file movement is handled by Amtech Service Processor tasks and Welch-managed shares.
- The current environment is batch/file-driven, with expected flat-file patterns such as .dat and .cov.

## Public Product Lineage

Public sources indicate this lineage:

1. Corrugated Technologies Inc. was founded in 1981 and focused on software, planning, optimization, production management, supply-chain, and corrugated-industry tools.
2. EFI acquired / merged Corrugated Technologies Inc. into EFI Productivity Software in 2015.
3. EFI later sold its eProductivity Software packaging and print productivity software business to an affiliate of Symphony Technology Group in 2022.
4. Current public ePS messaging says CTI point solutions are now incorporated into ePS Corrugated Suite / CorrSuite.

Engineering implication: when searching Welch servers, shares, databases, vendor docs, or service configs, do not search only for "CTI". Also search for historical and current names:

- CTI
- Corrugated Technologies
- EPS / ePS / eProductivity
- EFI Productivity Software
- CorrSuite
- Corrugated Suite
- CorrPlan
- CorrTrim
- CorrTrac
- CorrLink / Corrlink
- CorrChain
- pkgWARE
- CBS / Corrugated Business System
- Escada
- Auto-Count / Auto-Count 4D
- PC-Topp

## Public Product Vocabulary

| Product/name | Publicly described role | Why it matters for CoroMES |
|---|---|---|
| CorrPlan | Converting scheduling / capacity planning / finite scheduling | Likely related to lineups, work center capacity, and converting schedules. |
| CorrTrim | Corrugator scheduling / trim optimization | Likely related to corrugator schedules, roll/trim decisions, and upstream production plan. |
| CorrTrac | Roll stock inventory management | Likely related to roll stock, paper usage, roll location/status, and clamp-truck workflows. |
| CorrLink | Public trademark text describes transferring/manipulating information between computers in corrugated box manufacturing; secondary sources describe corrugator data collection | Possible clue for data collection, machine interface, bridge files, or historical service names. |
| CorrChain | Supply chain management | Search for customer/order/supply-chain integration artifacts. |
| pkgWARE | CTI ERP offering for forest products/corrugated context | Search older docs, database objects, and file prefixes. |
| Corrugated Business System (CBS) | ePS corrugated ERP/business system | Current ePS term for ERP-side corrugated suite. |
| Escada | Corrugator control / process control / QCS portfolio | Relevant if Welch has corrugator control integration or historical EPS/EFI components. |
| Auto-Count 4D | Plant-floor production intelligence, equipment counts/status/speed | Relevant to production event capture and machine status concepts. |

## Amtech Encore Public Context

Amtech currently positions EnCore ERP for corrugated and folding carton manufacturing. Public Amtech pages describe functions such as order processing, integrated scheduling, cost estimating, plant data entry, billing, sales analysis, financials, barcode inventory, quality, manufacturing analytics, and APIs/integrations.

Engineering implication: in Welch's architecture, Amtech is likely authoritative for orders, customers, billing, and ERP-side master data, while CTI/EPS is likely authoritative for plant-floor execution, schedules actually used by the floor, production events, downtime, and some roll-stock/clamp-truck signals. Confirm each domain with Welch data lineage before coding.

## Integration Architecture Hypothesis

Use this as a starting hypothesis only:

```text
-------------------+        +-----------------------+
| Amtech Encore    | <----> | Amtech Task Server    |
| ERP/order system |        | AmtSRVProc.exe jobs   |
+---------+---------+        | CorrExpv2             |
          |                  +-----------+-----------+
          | file exports/imports                     |
          v                                          v
    +-----+------------------------------------------+-----+
    | Welch-managed CTI/Amtech staging shares              |
    | .dat / .cov / archive / error / processed folders    |
    +-----+------------------------------------------+-----+
          |                                          ^
          v                                          |
+---------+------------------------------------------+-----+
| CTI / EPS on CTI01 / svwpcti01                           |
| CTI System Bridge, CTI Service Manager, plant-floor MES   |
+----------------------------------------------------------+
```

## Search Terms for Discovery

When searching local servers, scheduled tasks, services, file shares, config files, database names, and logs, include:

```text
CTI
EPS
ePS
EFI
Corrugated
CorrPlan
CorrTrim
CorrTrac
CorrLink
Corrlink
CorrChain
pkgWARE
CBS
Escada
Auto-Count
AutoCount
CorrExp
CorrExpv2
AmtSRVProc
Service Processor
System Bridge
Bridge
Clamp
Roll
Trim
```

## Source-Aware Discovery Checklist

1. Export Windows services from CTI01 / svwpcti01 and the Amtech task server.
2. Search service names, display names, executable paths, and config folders using the terms above.
3. Export scheduled tasks on the Amtech task server, especially jobs invoking AmtSRVProc.exe or CorrExpv2.
4. Locate staging shares by reading task actions, service configs, INI/XML/config files, and operator runbooks.
5. Preserve folder listings with file names, sizes, last-write times, and extensions.
6. Collect representative .dat/.cov file sets for normal, exception/rework, and canceled/voided scenarios.
7. For each critical flow, record source event, source system, file(s), service/task run, target update, and reconciliation evidence.

## Coding-Agent Guardrails

- Start with file-based extraction because it mirrors the existing integration boundary.
- Add read-only database extraction only when files are incomplete, delayed, or lossy.
- Treat all source layouts as untrusted until validated against real Welch samples.
- Store raw source payloads immutably before parsing.
- Use source file name, file hash, row number, source timestamp, source system, and correlation ID for traceability.
- Make parsers strict enough to quarantine malformed files but tolerant enough to preserve unknown fields in raw metadata.
- Do not infer destructive business actions from missing records; canceled/voided/deleted states require explicit source evidence.
- Use idempotent upserts into CoroMES staging/canonical tables.
- Reconcile counts, quantities, hashes, and golden transactions before calling a sync successful.

## Suggested CoroMES Connector Shape

The CTI connector should be a separate program/project in the CoroMES solution, aligned with ADR-011.

Suggested components:

- `CoroMES.Integration.Cti` project or equivalent
- file watcher / scheduled file puller
- immutable raw-file landing service
- layout registry for .dat/.cov and other discovered formats
- parser pipeline
- canonical mapper
- reconciliation service
- quarantine/dead-letter service
- structured logs with correlation IDs
- read-only DB extractors added later behind interfaces

## Public Sources

- ePS CTI page: https://epssw.com/corrugated-technologies-cti
- ePS Corrugated MES page: https://go.epackagingsw.com/corrugated-manufacturing-execution-software-mes
- ePS Corrugated Suite page: https://epssw.com/corrugated-suite
- ePS resource library: https://epssw.com/resource-library
- ePS / EFI Corrugated Packaging Suite eBook: https://go.efi.com/rs/559-INV-406/images/efi_eps_corrugated_suite_ebook_us_en_uk.pdf
- SUN Automation ePS Corrugated Packaging eBook mirror: https://sunautomation.com/wp-content/uploads/2023/10/ePS_Packaging_eBook_Corrugated_Packaging_English_1222.pdf
- Packaging Impressions on EFI acquiring CTI: https://www.packagingimpressions.com/article/efi-acquires-corrugated-technologies/
- EFI company history noting CTI acquisition: https://www.efi.com/about-efi/
- EFI 2022 realignment / EPS sale PDF: https://www.efi.com/wp-content/uploads/sites/2/2023/09/efi_realignment_release_final_01042022_en_us.pdf
- Amtech home page: https://www.amtechsoftware.com/
- Amtech solutions page: https://www.amtechsoftware.com/solutions/
- Justia EPS Packaging US LLC trademarks: https://trademarks.justia.com/owners/eps-packaging-us-llc-6432226/
- Justia CorrPlan trademark: https://trademarks.justia.com/741/14/corrplan-74114374.html
- Justia CorrLink trademark: https://trademarks.justia.com/738/18/corrlink-73818076.html
- Justia CorrChain trademark: https://trademarks.justia.com/762/67/corrchain-76267645.html
- WhatTheyThink CorrExpo 2023 ePS article: https://whattheythink.com/news/116238-eproductivity-software-showcase-industry-leading-solutions-empower-corrugated-manufacturers-achieve-lean-manufacturing-successfully-navigate-market-challenges-correxpo-2023/
- Russian corrugated MES/ERP market article mentioning CTI modules and Amtech integration context: https://gofromagazine.com/vse-pod-kontrolem.html

## Open Questions for Welch

- Which CTI modules are actually licensed/deployed at Elkhart?
- Are CorrPlan, CorrTrim, CorrTrac, CorrLink, CorrChain, pkgWARE, CBS, Escada, or Auto-Count present in service names, database names, folders, or screens?
- What is the CTI database platform and schema owner?
- Are read-only reporting views available, or must the connector start with files only?
- Which file extensions besides .dat and .cov appear in the CTI/Amtech staging shares?
- Does .cov mean cover/control/companion file in Welch's implementation?
- What are the exact source-of-truth boundaries between Amtech and CTI for schedules, production, downtime, roll stock, and clamp-truck events?
- Which support group owns CorrExpv2, AmtSRVProc.exe tasks, CTI System Bridge, and CTI Service Manager restart decisions?
