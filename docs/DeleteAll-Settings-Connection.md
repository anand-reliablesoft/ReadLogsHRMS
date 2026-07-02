# DeleteAll flag — settings DB connection notes

Tracking issue: [#1](https://github.com/anand-reliablesoft/ReadLogsHRMS/issues/1)

## What DeleteAll does
A one-shot flag stored in the Access DB `Settings` table (`SettingName = 'DeleteAll'`, value `'1'`/`'0'`).
- `False` → read only new logs from each device (`ReadGeneralLogData`).
- `True`  → read *all* logs from each device (`ReadAllGLogData`).

After a successful run where it was `True`, `BatchProcessor` resets it to `False`
(`BatchProcessor/Program.cs:267-276`), so it is a "force full resync" switch.

## Known problem: settings reads can silently fail
`SettingsProvider` connects to Access differently from `DatabaseConnectionManager`
(used for data logging), and both differences can make the flag read as `False`
even when the DB says `1`:

| | Data logging (`DatabaseConnectionManager`) | Settings (`SettingsProvider`) |
|---|---|---|
| Path | resolved to exe dir (`DatabaseConnectionManager.cs:35,43-56`) | raw relative → resolves to process working dir (`SettingsProvider.cs:23`) |
| Provider | ACE 12.0 + **Jet 4.0 fallback** | ACE 12.0 only |
| On failure | throws (visible) | **swallowed → returns `false`** (`SettingsProvider.cs:56-60`) |

For a Windows service the working dir is usually `C:\Windows\System32`, so the
settings reader opens a different (missing) file, fails, and silently returns `false`.

## How to toggle the flag manually
Set the row in the Access DB the service actually uses:
```sql
UPDATE Settings SET SettingValue = '1' WHERE SettingName = 'DeleteAll';
```
`SettingValue` must be text `'1'` (strict compare `value == "1"` in `SettingsProvider.cs:48`).

## Current workaround
Set an **absolute** `AccessDbPath` in the `.exe.config` of **both** processes so the
settings reader and the data connection point at the same file:
- `DataCollectionService.exe.config`
- `BatchProcessor.exe.config`  ← required; this process resets `DeleteAll` to `0`

Absolute paths are safe everywhere — all path resolvers pass rooted paths through unchanged.

## Real fix (see issue #1)
Route `SettingsProvider` through `IDatabaseConnectionManager.GetAccessConnection()` so
path resolution + provider fallback are shared with data logging, and stop swallowing
the exception in `GetDeleteAllMode()`.
