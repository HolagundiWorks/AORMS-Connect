# AORMS Connect

Suite core desktop app for **AORMS** — single login, suite launcher, shared project
catalog, DB connector, and installer links.

| | |
| --- | --- |
| **Package id** | `in.aorms.connect` |
| **Canon** | [AORMS-CONNECT.md](https://github.com/HolagundiWorks/aorms/blob/main/docs/esti/AORMS-CONNECT.md) |
| **Hub** | [aorms](https://github.com/HolagundiWorks/aorms) |
| **Downloads** | [aorms.in/downloads](https://aorms.in/downloads) (Coming soon until D6) |

## Status

**C1 shell:** Sign in (hub Activate) · Suite apps Open/Get · local project catalog ·
Licence stub. Session broker for sibling apps = **C2**.

## Develop

```bat
git submodule update --init --recursive
build-winui.cmd
build-msix.cmd
```

Set `ESTI_HUB_URL` (default `http://127.0.0.1:4000`).

## Suite apps launched from Connect

- AStudio · AConsulting  
- AQC Estimation · AQC BBS · AQC Project Management  
- AADT  

Do **not** fork `bbs_engine` — pin [AQC](https://github.com/HolagundiWorks/AQC).
