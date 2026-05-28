param(
    [int]$Days = 1,
    [int64]$DailyBudgetTokens = 500000,
    [switch]$IncludeCosts
)

$ErrorActionPreference = "Stop"

$adminKey = $env:OPENAI_ADMIN_KEY
if ([string]::IsNullOrWhiteSpace($adminKey)) {
    $adminKey = $env:OPENAI_API_KEY
}

if ([string]::IsNullOrWhiteSpace($adminKey)) {
    throw "Set OPENAI_ADMIN_KEY, or OPENAI_API_KEY if it has organization usage permissions."
}

$now = Get-Date
$startLocal = $now.Date.AddDays(-1 * ($Days - 1))
$endLocal = $now.Date.AddDays(1)
$start = [int][DateTimeOffset]$startLocal.ToUniversalTime().ToUnixTimeSeconds()
$end = [int][DateTimeOffset]$endLocal.ToUniversalTime().ToUnixTimeSeconds()

$headers = @{
    Authorization = "Bearer $adminKey"
    "Content-Type" = "application/json"
}

$usageUri = "https://api.openai.com/v1/organization/usage/completions?start_time=$start&end_time=$end&bucket_width=1d&limit=$Days&group_by%5B%5D=model"
$usage = Invoke-RestMethod -Uri $usageUri -Headers $headers -Method Get

$rows = @()
foreach ($bucket in @($usage.data)) {
    foreach ($result in @($bucket.results)) {
        $inputTokens = [int64]($result.input_tokens ?? 0)
        $outputTokens = [int64]($result.output_tokens ?? 0)
        $cachedTokens = [int64]($result.input_cached_tokens ?? 0)
        $audioInputTokens = [int64]($result.input_audio_tokens ?? 0)
        $audioOutputTokens = [int64]($result.output_audio_tokens ?? 0)
        $totalTokens = $inputTokens + $outputTokens + $audioInputTokens + $audioOutputTokens

        $rows += [pscustomobject]@{
            date = ([DateTimeOffset]::FromUnixTimeSeconds([int64]$bucket.start_time).ToLocalTime().DateTime.ToString("yyyy-MM-dd"))
            model = $result.model
            requests = [int64]($result.num_model_requests ?? 0)
            input_tokens = $inputTokens
            cached_input_tokens = $cachedTokens
            output_tokens = $outputTokens
            input_audio_tokens = $audioInputTokens
            output_audio_tokens = $audioOutputTokens
            total_tokens = $totalTokens
        }
    }
}

$byModel = $rows |
    Group-Object model |
    ForEach-Object {
        $groupRows = @($_.Group)
        [pscustomobject]@{
            model = if ($_.Name) { $_.Name } else { "(unattributed)" }
            requests = [int64](($groupRows | Measure-Object requests -Sum).Sum ?? 0)
            input_tokens = [int64](($groupRows | Measure-Object input_tokens -Sum).Sum ?? 0)
            cached_input_tokens = [int64](($groupRows | Measure-Object cached_input_tokens -Sum).Sum ?? 0)
            output_tokens = [int64](($groupRows | Measure-Object output_tokens -Sum).Sum ?? 0)
            total_tokens = [int64](($groupRows | Measure-Object total_tokens -Sum).Sum ?? 0)
        }
    } |
    Sort-Object total_tokens -Descending

$totalTokens = [int64](($rows | Measure-Object total_tokens -Sum).Sum ?? 0)
$resultObject = [ordered]@{
    period = "Last $Days day(s)"
    start_local = $startLocal.ToString("yyyy-MM-dd")
    end_local_exclusive = $endLocal.ToString("yyyy-MM-dd")
    daily_budget_tokens = $DailyBudgetTokens
    total_tokens = $totalTokens
    remaining_today = if ($Days -eq 1) { [int64]($DailyBudgetTokens - $totalTokens) } else { $null }
    percent_used_today = if ($Days -eq 1 -and $DailyBudgetTokens -gt 0) { [math]::Round(($totalTokens / $DailyBudgetTokens) * 100, 1) } else { $null }
    by_model = @($byModel)
}

if ($IncludeCosts) {
    $costUri = "https://api.openai.com/v1/organization/costs?start_time=$start&end_time=$end&bucket_width=1d&limit=$Days"
    $costs = Invoke-RestMethod -Uri $costUri -Headers $headers -Method Get
    $resultObject.costs = $costs
}

$resultObject | ConvertTo-Json -Depth 10
