# skynet 游戏服（Linux）
# 多阶段构建：builder 里编译，运行镜像只保留运行所需文件

FROM ubuntu:22.04 AS builder

ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update \
 && apt-get install -y --no-install-recommends build-essential ca-certificates \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# 与线上一致：静态 Lua + 不用 jemalloc
# 先 clean，避免把宿主机的中间产物带进来
RUN make clean >/dev/null 2>&1 || true
RUN make linux -j"$(nproc)" MALLOC_STATICLIB= SKYNET_DEFINES=-DNOUSE_JEMALLOC


FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive \
    TZ=Asia/Shanghai

RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates tzdata \
 && rm -rf /var/lib/apt/lists/* \
 && ln -sf /usr/share/zoneinfo/$TZ /etc/localtime \
 && echo $TZ > /etc/timezone

WORKDIR /app
COPY --from=builder /src /app
COPY deploy/docker/config.docker /app/mygame/config.docker

RUN useradd -r -u 10001 -m aiserver \
 && chown -R aiserver:aiserver /app

USER aiserver

EXPOSE 8088/tcp 8088/udp 5080/tcp

CMD ["./skynet", "mygame/config.docker"]