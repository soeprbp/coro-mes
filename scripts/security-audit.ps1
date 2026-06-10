param(
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$FailOnHeuristicFindings
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$failed = $false
$heuristicFindings = New-Object System.Collections.Generic.List[string]

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Script
    )

    Write-Host ""
    Write-Host "== $Name =="
    & $Script
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

function Add-Finding {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $script:heuristicFindings.Add($Message)
    Write-Host "Finding: $Message"
}

function Get-RepoRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $rootPath = $script:repoRoot.ProviderPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $fullPath = [System.IO.Path]::GetFullPath($Path)

    if ($fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($rootPath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    }

    return $fullPath
}

Invoke-Step "Restore" { dotnet restore CoroMES.sln }

if (-not $SkipBuild) {
    Invoke-Step "Build" { dotnet build CoroMES.sln --no-restore }
}

if (-not $SkipTests) {
    $testArgs = @("test", "CoroMES.sln")
    if (-not $SkipBuild) {
        $testArgs += "--no-build"
    }

    Invoke-Step "Tests" { dotnet @testArgs }
}

Write-Host ""
Write-Host "== NuGet vulnerability audit =="
$auditOutput = & dotnet list CoroMES.sln package --vulnerable --include-transitive 2>&1
$auditText = $auditOutput -join [Environment]::NewLine
Write-Host $auditText

if ($LASTEXITCODE -ne 0) {
    $failed = $true
    Add-Finding "NuGet audit command failed for CoroMES.sln."
}
elseif ($auditText -match "has the following vulnerable packages") {
    $failed = $true
    Add-Finding "Known vulnerable package reported for CoroMES.sln."
}

Write-Host ""
Write-Host "== Secret and unsafe-default heuristics =="
$scanFiles = Get-ChildItem -Recurse -File |
    Where-Object {
        $_.FullName -notmatch "\\(\.git|bin|obj|TestResults|coverage|packages|node_modules)\\" -and
        $_.Extension -notin @(".dll", ".exe", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".db", ".db-shm", ".db-wal", ".sqlite", ".sqlite-shm", ".sqlite-wal", ".sqlite3", ".sqlite3-shm", ".sqlite3-wal")
    }

$patterns = @(
    @{ Name = "private-key"; Regex = "BEGIN (RSA|DSA|EC|OPENSSH|PGP) PRIVATE KEY" },
    @{ Name = "cloud-access-key"; Regex = "AKIA[0-9A-Z]{16}" },
    @{ Name = "github-token"; Regex = "gh[pousr]_[A-Za-z0-9_]{20,}" },
    @{ Name = "openai-key"; Regex = "sk-[A-Za-z0-9]{20,}" },
    @{ Name = "credential-setting"; Regex = "(?i)\b(password|api[_-]?key|secret|token)\b\s*[:=]\s*[""']?([^""'\s{}$][^""'\r\n]*)" },
    @{ Name = "anonymous-mqtt"; Regex = "(?i)allow_anonymous\s+true" },
    @{ Name = "wildcard-hosts"; Regex = '"AllowedHosts"\s*:\s*"\*"' }
)

foreach ($file in $scanFiles) {
    $relativePath = Get-RepoRelativePath $file.FullName
    $lineNumber = 0

    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        $lineNumber++

        foreach ($pattern in $patterns) {
            if ($line -match $pattern.Regex) {
                $trimmed = $line.Trim()
                if ($trimmed -match "\$\{[A-Z0-9_]+\}" -or $trimmed -match ':\s*""\s*$' -or $trimmed -match '=\s*""\s*$') {
                    continue
                }

                Add-Finding "$($pattern.Name) heuristic matched at ${relativePath}:$lineNumber. Review without copying the sensitive value into logs."
            }
        }
    }
}

if ($heuristicFindings.Count -eq 0) {
    Write-Host "No heuristic findings."
}

Write-Host ""
Write-Host "== Security audit summary =="
Write-Host "Heuristic findings: $($heuristicFindings.Count)"

if ($failed -or ($FailOnHeuristicFindings -and $heuristicFindings.Count -gt 0)) {
    Write-Error "Security audit completed with findings."
    exit 1
}

Write-Host "Security audit completed."
