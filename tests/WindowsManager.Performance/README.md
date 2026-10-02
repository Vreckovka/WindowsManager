# WindowsManager performance checks

The primary benchmark runs the complete WindowsManager WPF application, original shell/resources/menus, full-size dimming windows and real DDC/CI brightness controllers. It navigates Monitors and Processes, records CPU and dispatcher latency, repeatedly closes dimmers through both close paths, and alternates idle/dimmed periods. Every fifth round also measures the process page.

The original component fixture remains available for isolated diagnosis, but its reduced layout and small overlays are not used for the real-app comparison.

## Build

Use Visual Studio MSBuild with the cached sibling-project outputs on this workstation:

    & 'C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe' WindowsManager/WindowsManager.csproj /t:Build /p:Configuration=Release /p:Platform=x64 /p:BuildProjectReferences=false /p:PerformanceTests=true /v:minimal /nologo

Add /p:PerformanceRegression=true to include assertions for blocked brightness drivers, bounded writes, duplicate overlays, timer restart/stop, fractional dimmed time, process sorting/filtering/favorites, hidden-view polling, linked monitor countdowns, automatic dimming and disposal.

Without PerformanceTests=true, the diagnostics entry point and harness are excluded from the production application.

## Run the real app

Only one WindowsManager instance may run. Set the working directory to the installed app data directory on this workstation, then launch a staged app with:

    WindowsManager.exe --performance-test D:\Aplikacie\WindowsManager\.performance\results\my-run 2400

The test currently uses the installed monitor-data location D:\Moje applikacie\Builds\WindowsManager\Data\Monitors. It copies monitor settings/counters into the output directory before measured cycles and redirects subsequent monitor writes there. Test brightness changes affect real monitors. The harness restores brightness when closing overlays. Startup still executes the application's normal data/backup routines.

The baseline build switch PerformanceBaseline=true uses the seven source/XAML snapshots under ignored .performance/baseline-source, captured from the pre-change checkout at 7624570d162346bd990211980c114ea6786f5f5f. Reconstruct those files from that commit when reproducing elsewhere. It skips candidate shutdown cleanup and otherwise uses the same diagnostics and dependencies. This is a rebuilt source baseline, not the installed 2025 binary.

## Outputs and comparison

Each run writes atomically updated results.json, screens.png, and processes.png. Acceptance requires a fresh completed result with success=true, no cleanup error, matching assembly hash, and mode complete application. An intermediate results file is not completion evidence.

    ./tests/WindowsManager.Performance/Write-Comparison.ps1 -BasePath <base-results.json> -NewPath <new-results.json> -OutputPath <report.md>

The report always has exactly two columns, Base and New. CPU is normalized over logical processors. UI-close timings exclude awaited background driver completion, which is reported separately. P95 UI delays pool recorded individual probes across each scenario; CPU and notification rates are weighted by actual sample duration.

Run-Comparison.ps1 accepts an absolute JSON configuration containing a unique job ID, durationSeconds, workingDirectory, reportScript, trusted current threadId, codexExe and two phases with name/exe/sha256/output. It verifies staged hashes and fresh results, waits outside the model, enforces per-phase deadlines and queues one completion message to the existing chat. It writes an exclusive listener lock, atomic status/result, stdout/stderr and queue status under the job directory. Do not reuse a job directory or run another application instance during a comparison.