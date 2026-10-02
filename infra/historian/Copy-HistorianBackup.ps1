<#
.SYNOPSIS
Pull a fresh PostgreSQL historian backup onto the Windows VM host.
.DESCRIPTION
Runs with a pre-authorized SSH key and pinned host key. The guest stages only
the database dump, never credentials. SCP downloads to a partial filename;
SHA256 and length must match before publication. A nonzero exit makes Task
Scheduler report failure. Existing successful backups are never overwritten.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$VmHost,
    [Parameter(Mandatory)][string]$SshUser,
    [Parameter(Mandatory)][string]$IdentityFile,
    [Parameter(Mandatory)][string]$KnownHostsFile,
    [Parameter(Mandatory)][string]$BackupDirectory
)
$ErrorActionPreference='Stop'
try {
    if ($SshUser -notmatch '^[a-z_][a-z0-9_-]*$' -or $VmHost -notmatch '^[a-zA-Z0-9.-]+$') { throw 'Invalid SSH account or hostname' }
    $ssh='C:\Program Files\OpenSSH\ssh.exe'
    $scp='C:\Program Files\OpenSSH\scp.exe'
    $options=@('-i',$IdentityFile,'-o','BatchMode=yes','-o','StrictHostKeyChecking=yes','-o',"UserKnownHostsFile=$KnownHostsFile",'-o','ConnectTimeout=10','-o','ServerAliveInterval=15','-o','ServerAliveCountMax=2')
    New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null
    $ErrorActionPreference='Continue'
    $raw=& $ssh @options "$SshUser@$VmHost" "sudo -n python3 /opt/coromes-historian/stage-host-backup.py $SshUser" 2> (Join-Path $BackupDirectory 'ssh-last-error.log')
    $ErrorActionPreference='Stop'
    if($LASTEXITCODE -ne 0){throw 'Guest backup/staging failed'}
    $manifest=($raw -join "`n") | ConvertFrom-Json
    if($manifest.name -notmatch '^history-\d{8}T\d{6}Z\.dump$' -or $manifest.sha256 -notmatch '^[a-f0-9]{64}$' -or $manifest.path -ne "/home/$SshUser/.coromes-backup-export/$($manifest.name)"){throw 'Unexpected backup manifest'}
    $final=Join-Path $BackupDirectory $manifest.name
    $partial="$final.partial"
    if((Test-Path -LiteralPath $final) -or (Test-Path -LiteralPath $partial)){throw 'Destination already exists; refusing overwrite'}
    & $scp @options "${SshUser}@${VmHost}:$($manifest.path)" $partial
    if($LASTEXITCODE -ne 0){throw 'Backup transfer failed'}
    if((Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.sha256 -or (Get-Item -LiteralPath $partial).Length -ne $manifest.bytes){throw 'Backup checksum or length mismatch'}
    Move-Item -LiteralPath $partial -Destination $final
    "$($manifest.sha256)  $($manifest.name)" | Set-Content -LiteralPath "$final.sha256" -Encoding Ascii
    [pscustomobject]@{SavedUtc=[DateTime]::UtcNow.ToString('o');File=$final;Bytes=$manifest.bytes;SHA256=$manifest.sha256} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $BackupDirectory 'last-success.json') -Encoding UTF8
    Write-Output "Verified historian backup saved: $final"
} catch {
    # Preserve a short failure record for unattended Task Scheduler runs.
    if (Test-Path -LiteralPath $BackupDirectory) {
        [pscustomobject]@{FailedUtc=[DateTime]::UtcNow.ToString('o');Error=$_.Exception.Message} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $BackupDirectory 'last-failure.json') -Encoding UTF8
    }
    Write-Error $_
    exit 1
}
