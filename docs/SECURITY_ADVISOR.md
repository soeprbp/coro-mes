# CoroMES Security Advisor

Use this advisor before releases, after dependency upgrades, and whenever new API, integration, industrial protocol, or agent-facing behavior is added.

## Runbook

```powershell
.\scripts\security-audit.ps1
```

For a stricter local gate:

```powershell
.\scripts\security-audit.ps1 -FailOnHeuristicFindings
```

## Review Scope

- API endpoints: authentication, authorization, input validation, rate limits, unsafe verbs, and public static files.
- Data access: raw SQL usage, over-broad query results, migration safety, connection string handling, and least-privilege database users.
- Integrations: CTI/EPS file ingestion, path handling, raw payload retention, quarantine behavior, external API keys, and outbound HTTP timeouts.
- Industrial protocols: MQTT anonymous access, OPC UA authentication mode, PLC write operations, and network exposure.
- Agent-facing surfaces: MCP/tools must enforce authentication, plant/role scoping, audit logging, explicit write permissions, and human approval for destructive actions.
- Dependencies and CI: NuGet vulnerability audit, build, tests, and secret heuristics.

## Baseline Findings To Track

- High: unauthenticated API endpoints can create, update, and delete operational MES records. Add ASP.NET Core authentication/authorization before non-local deployment.
- Medium: development config uses wildcard hosts, sample database passwords, anonymous MQTT, and HTTP i3X defaults. Keep these local-only and require environment overrides for shared environments.
- Medium: CTI raw/quarantine/archive payloads may contain customer, schedule, production, or order data. Keep payload directories out of Git and restrict filesystem permissions.
- Medium: browser admin UI has client-side-only password gating. Treat it as a prototype until it is backed by server-side auth.

## Reporting Format

- Risk Level: Critical/High/Medium/Low
- Vulnerability: brief description of the flaw
- Impact: what could happen if exploited
- Remediation: exact code changes or configuration fixes required
