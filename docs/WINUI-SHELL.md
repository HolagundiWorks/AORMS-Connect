# AORMS Connect — WinUI shell (Manager Hub)

**Status:** Manager Hub chrome · **Updated:** 2026-08-11

Suite-core desktop host — **Manager Hub**, not a practice manager. Uses
`Themes/HcwTheme.xaml` (AStudio SoT copy) at density **1×** (no `ScaleHost`).

**Canon:** [AORMS-CONNECT.md](https://github.com/HolagundiWorks/aorms/blob/main/docs/esti/AORMS-CONNECT.md) ·
[DESKTOP-WINUI-UX.md](https://github.com/HolagundiWorks/aorms/blob/main/docs/esti/DESKTOP-WINUI-UX.md)

| Region | Contents |
| --- | --- |
| MenuBar | File (Refresh · Exit) · Help (About · Canon) |
| Ribbon 56 | **AORMS Connect** once · Manager Hub · sync chip · Local AI |
| Stage | Managers · Technical · Drafting tiles · Projects ListView · Account & hub · DB expander |
| ActionDock ≤3 | Add project · **Flush** (primary) · Activate |
| Taskbar 60 | Downloads · Refresh outbox · Copy install id |

## Launch groups

| Group | Apps |
| --- | --- |
| Managers | AStudio · AConsulting |
| Technical | **AQC Core** · Estimation · BBS · PM |
| Drafting | AADT |

**Licence:** Activate only here → writes `%LocalAppData%\AORMS-Connect\session.json`.
Suite apps import via `TryImportConnectSession` / `--connect-session` (no per-app HLP Activate).
AADT imports into `%LocalAppData%\AADT\firm.db` via `aadt_bridge.dll` (Aorms.Bridge).

Open passes `--connect-session` when `session.json` exists (`AAD.exe` → `app --connect-session …`).
Dev builds also resolve sibling `Repos/` unpackaged Release paths (incl. `AadWinui.exe` / `AAD.exe`).

## Build

```bat
build-winui.cmd
```
