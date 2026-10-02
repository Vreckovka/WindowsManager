param([Parameter(Mandatory=$true)][string]$ConfigPath)
$ErrorActionPreference='Stop'
$config=Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$jobDir=Split-Path -Parent $ConfigPath
$lock=[IO.File]::Open((Join-Path $jobDir 'listener.lock'),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
function Save-Atomic($path,$value) {
  [IO.File]::WriteAllText(($path+'.tmp'),($value | ConvertTo-Json -Depth 12))
  [IO.File]::Move(($path+'.tmp'),$path,$true)
}
$state=[ordered]@{jobId=$config.jobId;listenerPid=$PID;startedUtc=[DateTime]::UtcNow.ToString('o');state='running';phases=@();phase=$null}
$child=$null
try {
  Save-Atomic (Join-Path $jobDir 'status.json') $state
  foreach($phase in $config.phases) {
    $phaseStart=[DateTime]::UtcNow
    $dll=Join-Path (Split-Path -Parent $phase.exe) 'WindowsManager.dll'
    $hash=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    if($hash -ne $phase.sha256) { throw 'Staged assembly changed before launch.' }
    New-Item -ItemType Directory -Path $phase.output -Force | Out-Null
    $child=Start-Process -FilePath $phase.exe -ArgumentList @('--performance-test',$phase.output,[string]$config.durationSeconds) -WorkingDirectory $config.workingDirectory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $phase.output 'stdout.log') -RedirectStandardError (Join-Path $phase.output 'stderr.log') -PassThru
    $identity=$child.StartTime.ToUniversalTime()
    $state.phase=$phase.name
    $state.testPid=$child.Id
    $state.testStartedUtc=$identity.ToString('o')
    Save-Atomic (Join-Path $jobDir 'status.json') $state
    $deadline=$phaseStart.AddSeconds($config.durationSeconds+300)
    while(-not $child.WaitForExit(60000)) {
      if([DateTime]::UtcNow -gt $deadline) {
        $live=Get-Process -Id $child.Id -ErrorAction SilentlyContinue
        if($live -and $live.StartTime.ToUniversalTime() -eq $identity) { Stop-Process -Id $live.Id }
        $state.state='deadline exceeded'
        throw ('Deadline exceeded in '+$phase.name)
      }
    }
    $child.WaitForExit()
    $resultPath=Join-Path $phase.output 'results.json'
    if(-not (Test-Path -LiteralPath $resultPath)) { throw ('Missing results: '+$phase.name) }
    $result=Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    if(-not $result.success -or $result.cleanupError -or -not $result.completedUtc -or [DateTime]$result.startedUtc -lt $phaseStart.AddSeconds(-1) -or $result.assemblySha256 -ne $hash -or $result.mode -ne 'complete application' -or $result.durationSeconds -ne $config.durationSeconds) {
      throw ('Fresh-result verification failed: '+$phase.name)
    }
    $state.phases+=@{name=$phase.name;exitCode=$child.ExitCode;sha256=$hash;resultPath=$resultPath;completedUtc=$result.completedUtc}
    Save-Atomic (Join-Path $jobDir 'status.json') $state
    if($child.ExitCode -ne 0) { throw ('App exit failed: '+$phase.name) }
    $child=$null
  }
  & $config.reportScript -BasePath $state.phases[0].resultPath -NewPath $state.phases[1].resultPath -OutputPath (Join-Path $jobDir 'comparison.md')
  $state.state='success'
} catch {
  if($state.state -eq 'running') { $state.state='failure' }
  $state.error=$_.ToString()
} finally {
  $state.completedUtc=[DateTime]::UtcNow.ToString('o')
  $state.phase=$null
  Save-Atomic (Join-Path $jobDir 'result.json') $state
  Save-Atomic (Join-Path $jobDir 'status.json') $state
  $message='Job '+$config.jobId+' finished with '+$state.state+'. Read '+(Join-Path $jobDir 'result.json')+', verify fresh baseline and candidate measurements and screenshots, then finish the authorized WindowsManager freeze fix and performance comparison. This notification does not authorize additional work.'
  try {
    & $config.codexExe queue --thread $config.threadId --message $message *> (Join-Path $jobDir 'queue.log')
    Save-Atomic (Join-Path $jobDir 'queue-status.json') @{attemptedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$LASTEXITCODE;jobId=$config.jobId}
  } catch {
    Save-Atomic (Join-Path $jobDir 'queue-status.json') @{attemptedUtc=[DateTime]::UtcNow.ToString('o');error=$_.ToString();jobId=$config.jobId}
  }
  $lock.Dispose()
}