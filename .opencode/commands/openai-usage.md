# OpenAI Usage Command

Show OpenAI usage from the OpenAI Organization Usage API. Do not use ModelRelay or local OpenCode alias data for this command.

## Usage

`/openai-usage [days]`

If no day count is provided, use `1`.

## Workflow

1. Run the project script:

```powershell
$days = if ("$ARGUMENTS" -match '^\d+$') { [int]"$ARGUMENTS" } else { 1 }
.\scripts\openai-usage.ps1 -Days $days
```

2. If the script reports a missing key, tell the user to set `OPENAI_ADMIN_KEY` with an OpenAI Admin API key that has organization usage permissions.

3. Report:

- Period checked.
- Requests.
- Model names.
- Input tokens.
- Output tokens.
- Cached input tokens.
- Total tokens.

4. Do not include `router/auto-fastest`, ModelRelay, or OpenCode local usage unless the user explicitly asks for local OpenCode usage.
