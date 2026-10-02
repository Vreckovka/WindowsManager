# WindowsManager release 5.5.9771.4550

The sustained comparison was cancelled at the user request. These are the completed short full-app runs on three real monitors; no sustained-test completion or two-hour validation is claimed. The release includes the freeze and process-list optimizations, the original full application UI, and no compiled performance-test entry point.

Base is original source at commit 7624570d162346bd990211980c114ea6786f5f5f rebuilt with the same cached dependencies. CPU measures this application process normalized across eight logical processors. UI delay is an Input-priority dispatcher probe. Close measurements time the UI call; candidate native brightness work is asynchronous, so these timings do not establish faster hardware completion.

| Base | New |
|---|---|
| Released assembly version: 2.7.8710.34866 | Released assembly version: 5.5.9771.4550 |
| idle sample time: 22.56 s | idle sample time: 23.06 s |
| idle CPU: 0.28% | idle CPU: 0.11% |
| idle UI delay P95: 0.76 ms | idle UI delay P95: 0.57 ms |
| idle UI delay maximum: 2.07 ms | idle UI delay maximum: 4.74 ms |
| idle sampled private memory: 119.61 MiB | idle sampled private memory: 113.64 MiB |
| processesVisible sample time: 25.19 s | processesVisible sample time: 22.81 s |
| processesVisible CPU: 1.53% | processesVisible CPU: 0.15% |
| processesVisible UI delay P95: 246.00 ms | processesVisible UI delay P95: 0.55 ms |
| processesVisible UI delay maximum: 1037.06 ms | processesVisible UI delay maximum: 1.33 ms |
| processesVisible sampled private memory: 242.89 MiB | processesVisible sampled private memory: 194.07 MiB |
| dimmed sample time: 22.86 s | dimmed sample time: 22.88 s |
| dimmed CPU: 0.80% | dimmed CPU: 0.16% |
| dimmed UI delay P95: 2.02 ms | dimmed UI delay P95: 0.46 ms |
| dimmed UI delay maximum: 96.39 ms | dimmed UI delay maximum: 1.75 ms |
| dimmed sampled private memory: 289.28 MiB | dimmed sampled private memory: 224.18 MiB |
| Close UI P95/max, 8 cycles: 66.51 ms | Close UI P95/max, 8 cycles: 19.09 ms |
| Queued callbacks during 3 s UI stall: 109 | Queued callbacks during 3 s UI stall: 0 |
| Retained closed-overlay references: 3 | Retained closed-overlay references: 0 |

Candidate regression checks already passed for a blocked brightness driver, 1000 coalesced requests, duplicate-overlay prevention, repeated initialization, final fractional dimmed time, and 100 timer start/stop cycles. The additional extended regression suite was compiled but not run after the user cancelled testing. Real configuration had zero enabled monitor rules. Both versions logged existing torrent startup HTTP 403 errors.

Release version follows the existing VersionAutoIncrement.tt date/time formula; the template remains unchanged. Build uses the existing x64 Release FolderProfile. Replaced deployment files are backed up under .performance/release-backup-20261002-0231, with a manifest. App user data is excluded from replacement.