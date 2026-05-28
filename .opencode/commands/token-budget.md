# Token Budget Command

Check whether OpenAI Organization Usage API tokens are within a daily budget.

## Usage

`/token-budget [daily-token-budget]`

If no budget is provided, use `500000` OpenAI tokens per day.

## Workflow

1. Run:

```powershell
$budget = if ("$ARGUMENTS" -match '^\d+$') { [int64]"$ARGUMENTS" } else { 500000 }
.\scripts\openai-usage.ps1 -Days 1 -DailyBudgetTokens $budget
```

2. Summarize the result in plain English:

- If usage is under 50%, say it is comfortably under budget.
- If usage is 50-80%, say to be mindful.
- If usage is 80-100%, recommend avoiding OpenAI models unless needed.
- If usage is over budget, recommend switching away from OpenAI models for the rest of the day unless the task needs them.

3. Do not use ModelRelay or `opencode-usage` for this budget check. This command is specifically for OpenAI Organization Usage API accounting.
