param([Parameter(Mandatory=$true)][string]$BasePath,[Parameter(Mandatory=$true)][string]$NewPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$base=Get-Content -LiteralPath $BasePath -Raw | ConvertFrom-Json
$new=Get-Content -LiteralPath $NewPath -Raw | ConvertFrom-Json
function N($value,$places=2) { ([double]$value).ToString('F'+$places,[Globalization.CultureInfo]::InvariantCulture) }
function P95($values) {
  $sorted=@($values | Sort-Object)
  if(-not $sorted.Count) { return 0 }
  return $sorted[[Math]::Ceiling($sorted.Count*.95)-1]
}
function Phase($r,$pattern) {
  $s=@($r.samples | Where-Object name -Match $pattern)
  $wall=($s.wallSeconds | Measure-Object -Sum).Sum
  $cpu=($s.cpuMs | Measure-Object -Sum).Sum/($wall*1000)/$r.processorCount*100
  $probes=@($s | ForEach-Object { $_.dispatcherLatenciesMs })
  return @{wall=$wall;cpu=$cpu;p95=(P95 $probes);max=($probes | Measure-Object -Maximum).Maximum;notifications=($s.notifications | Measure-Object -Sum).Sum/$wall}
}
$rows=[Collections.Generic.List[string]]::new()
$rows.Add('| Base | New |')
$rows.Add('|---|---|')
$rows.Add('| Actual app runtime: '+(N (([DateTime]$base.completedUtc-[DateTime]$base.startedUtc).TotalSeconds))+' s | Actual app runtime: '+(N (([DateTime]$new.completedUtc-[DateTime]$new.startedUtc).TotalSeconds))+' s |')
foreach($case in @(@{name='Idle';pattern='^(idle|soakIdle_)'},@{name='Processes visible';pattern='^(processesVisible|soakProcesses_)'},@{name='Dimmed';pattern='^(dimmed|soakDimmed_)'})) {
  $b=Phase $base $case.pattern
  $n=Phase $new $case.pattern
  foreach($metric in @(@{label='sample time';key='wall';unit=' s'},@{label='CPU';key='cpu';unit='%'},@{label='UI delay P95';key='p95';unit=' ms'},@{label='UI delay maximum';key='max';unit=' ms'},@{label='binding notifications';key='notifications';unit='/s'})) {
    $label=$case.name+' '+$metric.label
    $rows.Add('| '+$label+': '+(N $b[$metric.key])+$metric.unit+' | '+$label+': '+(N $n[$metric.key])+$metric.unit+' |')
  }
}
foreach($metric in @(@{label='Overlay close UI P95';b=(P95 $base.closeMs);n=(P95 $new.closeMs);unit=' ms'},@{label='Overlay close UI maximum';b=$base.closeMaxMs;n=$new.closeMaxMs;unit=' ms'},@{label='Close through brightness completion P95';b=(P95 $base.closeThroughBrightnessMs);n=(P95 $new.closeThroughBrightnessMs);unit=' ms'},@{label='Close cycles';b=$base.closeMs.Count;n=$new.closeMs.Count;unit=''},@{label='Queued callbacks during 3 s UI stall';b=$base.operationsQueuedDuring3sStall;n=$new.operationsQueuedDuring3sStall;unit=''},@{label='Retained overlay references';b=$base.retainedOverlayReferences;n=$new.retainedOverlayReferences;unit=''},@{label='Timer drift at last tick';b=$base.timerDriftAtTickMs;n=$new.timerDriftAtTickMs;unit=' ms'},@{label='Ticks after stopping';b=$base.ticksAfterStop;n=$new.ticksAfterStop;unit=''},@{label='Peak sampled private memory';b=(($base.samples.privateBytes | Measure-Object -Maximum).Maximum/1MB);n=(($new.samples.privateBytes | Measure-Object -Maximum).Maximum/1MB);unit=' MiB'},@{label='Final private memory';b=$base.endPrivateBytes/1MB;n=$new.endPrivateBytes/1MB;unit=' MiB'},@{label='Final handles';b=$base.endHandles;n=$new.endHandles;unit=''},@{label='Final threads';b=$base.endThreads;n=$new.endThreads;unit=''},@{label='Launch to menus ready';b=$base.launchToMenuReadyMs;n=$new.launchToMenuReadyMs;unit=' ms'})) {
  $rows.Add('| '+$metric.label+': '+(N $metric.b)+$metric.unit+' | '+$metric.label+': '+(N $metric.n)+$metric.unit+' |')
}
$content='# WindowsManager sustained performance comparison'+[Environment]::NewLine+[Environment]::NewLine
$content+='Both phases run the complete real application, its original menus and layout, and real brightness controllers on three monitors. Baseline is unmodified pre-change source at commit 7624570d162346bd990211980c114ea6786f5f5f rebuilt with the same cached dependencies as the candidate; it is not a byte-identical copy of the installed February 2025 binary.'+[Environment]::NewLine+[Environment]::NewLine
$content+='CPU is this application process time normalized across eight logical processors. UI delay is an Input-priority dispatcher probe every 200 ms; pooled P95 values use individual recorded probes. Hardware completion is measured separately from the UI close call. Samples cover idle, dimmed overlays, and the visible process page. Periodic close tests alternate the command and actual window Closed event. Tests use copied monitor settings/counters.'+[Environment]::NewLine+[Environment]::NewLine
$content+='These sequential runs see a changing live Windows process list and system load. No monitor rules were enabled in the real user configuration. Existing torrent startup HTTP 403 errors were observed during smoke tests. Startup and maxima can include unrelated I/O and driver latency. This is an approximately 40-minute-per-version comparison, not a two-hour-per-version soak.'+[Environment]::NewLine+[Environment]::NewLine
$content+=($rows -join [Environment]::NewLine)+[Environment]::NewLine+[Environment]::NewLine
$content+='Base binary SHA256: '+$base.assemblySha256+[Environment]::NewLine+'New binary SHA256: '+$new.assemblySha256+[Environment]::NewLine
$content+='Base raw results: '+$BasePath+[Environment]::NewLine+'New raw results: '+$NewPath+[Environment]::NewLine
[IO.File]::WriteAllText($OutputPath,$content)