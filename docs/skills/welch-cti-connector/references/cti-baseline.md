# CTI Baseline Reference

## Welch Facts

- CTI means Corrugated Technologies Inc. / EPS, not telephony.
- CTI is used for Elkhart plant-floor and scheduling workflows.
- CTI integrates with Amtech Encore.
- CTI System Bridge runs on CTI01 / svwpcti01 and is the CTI-Amtech import/export bridge.
- CTI Service Manager on CTI01 is primarily for clamp-truck workflows.
- CorrExpv2 on the Amtech task server may stop and need restart during bridge incidents.
- Amtech Encore uses Amtech Service Processor tasks, including AmtSRVProc.exe, plus Welch-managed file shares.
- Welch does not use a local TrueCommerce Integration Service/agent for EDI file movement.
- The environment is batch/file-driven. Expect flat files such as .dat and .cov.

## Public Product Lineage

- Corrugated Technologies Inc. was founded in 1981 and focused on corrugated planning, optimization, production management, and supply-chain tools.
- EFI acquired / merged CTI into EFI Productivity Software in 2015.
- EFI sold the eProductivity Software packaging and print productivity software business to an STG affiliate in 2022.
- Current public ePS messaging says CTI point solutions are now part of Corrugated Suite / CorrSuite.

## Names to Search

Search local services, tasks, file shares, config files, database objects, vendor docs, and logs for:

```text
CTI
EPS
ePS
EFI
eProductivity
Corrugated
CorrSuite
Corrugated Suite
CorrPlan
CorrTrim
CorrTrac
CorrLink
Corrlink
CorrChain
pkgWARE
CBS
Corrugated Business System
Escada
Auto-Count
AutoCount
PC-Topp
CorrExp
CorrExpv2
AmtSRVProc
Service Processor
System Bridge
Clamp
Roll
Trim
```

## Product Vocabulary

| Name | Public role | Connector implication |
|---|---|---|
| CorrPlan | Converting scheduling / capacity planning | Search for lineups, converting schedules, work center capacity. |
| CorrTrim | Corrugator scheduling / trim optimization | Search for corrugator lineup, trim, roll, and upstream scheduling data. |
| CorrTrac | Roll stock inventory management | Search for roll status, location, usage, clamp-truck signals. |
| CorrLink | Transfer/manipulation between computers; secondary sources mention corrugator data collection | Search for data collection, machine interface, bridge, and file movement artifacts. |
| CorrChain | Supply chain management | Search order, customer, supply, and demand flow artifacts. |
| pkgWARE | CTI ERP offering | Search older docs, prefixes, and database names. |
| CBS | Corrugated Business System | Search ERP/business system context in ePS docs. |
| Escada | Corrugator process control / QCS | Relevant if corrugator control data is present. |
| Auto-Count 4D | Plant-floor production intelligence | Relevant to counts, status, speed, and equipment data. |

## Likely Source-of-Truth Boundaries

Verify before coding:

- Amtech likely owns customers, orders, billing, ERP master data.
- CTI/EPS likely owns shop-floor execution, schedules used by the floor, production events, downtime, and some roll/clamp-truck signals.
- File drops likely express the legacy integration contract; database reads may be needed for fields that never appear in files.

## Public Sources

- https://epssw.com/corrugated-technologies-cti
- https://go.epackagingsw.com/corrugated-manufacturing-execution-software-mes
- https://epssw.com/corrugated-suite
- https://epssw.com/resource-library
- https://www.packagingimpressions.com/article/efi-acquires-corrugated-technologies/
- https://www.efi.com/about-efi/
- https://www.efi.com/wp-content/uploads/sites/2/2023/09/efi_realignment_release_final_01042022_en_us.pdf
- https://www.amtechsoftware.com/
- https://www.amtechsoftware.com/solutions/
- https://trademarks.justia.com/owners/eps-packaging-us-llc-6432226/
- https://whattheythink.com/news/116238-eproductivity-software-showcase-industry-leading-solutions-empower-corrugated-manufacturers-achieve-lean-manufacturing-successfully-navigate-market-challenges-correxpo-2023/
- https://gofromagazine.com/vse-pod-kontrolem.html
