# skynet 上游记录

本项目直接 vendor 了 skynet 源码（方案 A），不再作为 submodule 管理。

| 项 | 值 |
| --- | --- |
| 来源 | https://github.com/cloudwu/skynet |
| commit | `64391f732e76507ea6615f312cb71be1fd7bfbdd` |
| 提交说明 | Reserve PTYPE_TRACE in skynet.h (#2170) |
| 获取方式 | `git clone --depth 1 https://github.com/cloudwu/skynet.git` |

## 本地修改（必须保留）

| 文件 | 改动 | 原因 |
| --- | --- | --- |
| `3rd/compat-mingw/unistd.c` | `clock_gettime` 用 `#ifndef WIN_PTHREADS_TIME_H` 包住 | 新版 mingw-w64 的 winpthreads 已提供该函数，重复定义会导致 Windows 编译失败 |
| `skynet-src/socket_server.c` | `forward_message_udp` 的 `recvfrom` 失败分支增加 `#ifdef _WIN32` 判断：`WSAEWOULDBLOCK` / `WSAECONNRESET` 视为可忽略 | winsock 不写 `errno`，非阻塞 UDP 读空会把 `EAGAIN` 误判为致命错误，直接 `force_close` 掉 UDP 监听 socket（Windows 下 KCP 完全不可用） |

## 新增的 luaclib

| 文件 | 说明 |
| --- | --- |
| `3rd/kcp/ikcp.c`、`ikcp.h`、`LICENSE` | vendor https://github.com/skywind3000/kcp（MIT），commit `b1a7a2101dcbb96017681a500d6b82bbe5a88766` |
| `lualib-src/lkcp.c` | `luaclib/kcp.so` 的绑定（拉模式：`update` 后 `flush()` 取数据报） |

`Makefile` / `mingw.mk` 的 `LUA_CLIB` 已加入 `kcp`。

## 升级步骤

1. 另开目录 clone 目标 commit 的上游代码；
2. 对比并覆盖 `skynet-src/`、`service-src/`、`lualib-src/`、`lualib/`、`service/`、`3rd/`、`Makefile`、`platform.mk`、`mingw.mk`；
3. 重新应用上表中的补丁；
4. Windows：`make mingw -j8`；Linux：`make linux -j4 MALLOC_STATICLIB= SKYNET_DEFINES=-DNOUSE_JEMALLOC`；
5. 跑 `mygame/test-*.ps1` 回归。

升级时**不要**覆盖 `mygame/`、`deploy/`、`run-server.bat`、`run-windows.bat`。