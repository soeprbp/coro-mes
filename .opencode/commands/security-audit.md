# Security Audit Command

Run the CoroMES security advisor workflow and report concrete, prioritized findings.

## Usage

`/security-audit`

## Workflow

1. Run the project audit script:

```powershell
.\scripts\security-audit.ps1
```

2. If the user wants CI-style strictness for heuristic findings, run:

```powershell
.\scripts\security-audit.ps1 -FailOnHeuristicFindings
```

3. Review the code manually using the security advisor checklist in `docs/SECURITY_ADVISOR.md`.

4. Report findings in this format:

- Risk Level: Critical/High/Medium/Low
- Vulnerability: concise description
- Impact: what can happen if exploited
- Remediation: exact code or configuration change

5. Do not print, copy, or store secret values. If a secret-like value is found, identify only the file, line number, and type of finding.
