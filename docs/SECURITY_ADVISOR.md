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
- Alerting: outbound email, SMS, Pushover, and UpKeep notification credentials, throttling, escalation loops, recipient privacy, and auditability.
- Settings: persisted admin settings must be allowlisted, non-secret, audited, and separated from startup/deployment secrets.
- Industrial protocols: MQTT anonymous access, OPC UA authentication mode, PLC write operations, and network exposure.
- Agent-facing surfaces: MCP/tools must enforce authentication, plant/role scoping, audit logging, explicit write permissions, and human approval for destructive actions.
- Dependencies and CI: NuGet vulnerability audit, build, tests, and secret heuristics.

## Baseline Findings To Track

- High: the standalone `CoroMES.Api` host still needs the same auth gate or should remain unexposed while `CoroMES.Web` is the forward host.
- Medium: the Blazor host has a first cookie-based admin gate, but it is not final enterprise identity. Replace access-code auth with the chosen identity provider before production use.
- Medium: audit logging currently covers equipment, display definitions, and first-pass UpKeep actions. Expand audit coverage to all work order, material, operator, live integration, and future write actions.
- Medium: live UpKeep mode requires API credentials and has guarded write stubs. Keep credentials in environment/user-secret/deployment configuration and confirm the live write contract before enabling writes.
- Medium: live alerting mode is intentionally guarded. Do not enable real email/SMS/Pushover/UpKeep sends until provider credentials, allow-listed recipients, throttling, escalation rules, failure handling, and audit expectations are configured outside source control.
- Medium: `SystemSettings` persists non-secret admin settings and feature flags only. Keep secret-looking values rejected, avoid arbitrary key editors, and do not treat DB flags as a stronger security boundary than the temporary admin access-code gate can support.
- Medium: development config uses wildcard hosts, sample database passwords, anonymous MQTT, and HTTP i3X defaults. Keep these local-only and require environment overrides for shared environments.
- Medium: CTI raw/quarantine/archive payloads may contain customer, schedule, production, or order data. Keep payload directories out of Git and restrict filesystem permissions.

## Reporting Format

- Risk Level: Critical/High/Medium/Low
- Vulnerability: brief description of the flaw
- Impact: what could happen if exploited
- Remediation: exact code changes or configuration fixes required
