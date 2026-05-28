# Token Usage Rules

- The user's primary concern is OpenAI model usage, not local ModelRelay usage.
- Do not infer OpenAI usage from ModelRelay aliases such as `router/auto-fastest`.
- When the user asks about OpenAI token usage, use the OpenAI Organization Usage API via `scripts/openai-usage.ps1`.
- Prefer `opencode-usage` for local OpenCode token checks.
- For OpenAI-specific checks, use `/openai-usage` or `/token-budget`.
- OpenAI usage checks require `OPENAI_ADMIN_KEY` or another key with organization usage permissions.
- Before intentionally switching to an OpenAI model for a large task, check `/token-budget` or clearly state that OpenAI usage may increase.
