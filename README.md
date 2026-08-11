# AORMS Connect — Manager Hub

Suite core desktop app for **AORMS** — single login, Manager Hub launcher, shared
project catalog, DB connector, and installer links.

| | |
| --- | --- |
| **Package id** | `in.aorms.connect` |
| **Canon** | [AORMS-CONNECT.md](https://github.com/HolagundiWorks/aorms/blob/main/docs/esti/AORMS-CONNECT.md) |
| **Shell** | [docs/WINUI-SHELL.md](docs/WINUI-SHELL.md) |
| **Hub** | [aorms](https://github.com/HolagundiWorks/aorms) |
| **Downloads** | [aorms.in/downloads](https://aorms.in/downloads) (Coming soon until D6) |

## Status

**Manager Hub chrome:** MenuBar · ribbon · Managers/Technical/Drafting tiles ·
Projects ListView · Account & hub · DB expander · ActionDock (Add · Flush · Activate) ·
taskbar. Session/catalog for siblings = **C2**. After Flush, browse on hub `/ops-db`.

## Develop

```bat
git submodule update --init --recursive
build-winui.cmd
build-msix.cmd
```

Set `ESTI_HUB_URL` (default `http://127.0.0.1:4000`).

## Suite apps launched from Connect

| Group | Apps |
| --- | --- |
| Managers | AStudio · AConsulting |
| Technical | **AQC Core** · Estimation · BBS · PM |
| Drafting | AADT |

Do **not** fork `bbs_engine` — pin [AQC](https://github.com/HolagundiWorks/AQC).
