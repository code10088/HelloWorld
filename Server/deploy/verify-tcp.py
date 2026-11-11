#!/usr/bin/env python3
"""发送一个 CSAuth 帧，检查游戏服是否返回 SCAuth(10001)+SCPlayerData(10003)。"""
import socket
import struct
import sys

host = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"
port = int(sys.argv[2]) if len(sys.argv) > 2 else 8088


def recv_exact(sock, size):
    data = b""
    while len(data) < size:
        chunk = sock.recv(size - len(data))
        if not chunk:
            raise SystemExit(f"连接提前关闭，只收到 {len(data)}/{size} 字节")
        data += chunk
    return data


def read_frame(sock):
    length = struct.unpack("<i", recv_exact(sock, 4))[0]
    body = recv_exact(sock, length)
    msg_id = struct.unpack("<H", body[:2])[0]
    return msg_id, body


token = b"test-access-token"
body = struct.pack("<H", 1) + struct.pack("<H", len(token)) + token

with socket.create_connection((host, port), timeout=5) as sock:
    sock.sendall(struct.pack("<i", len(body)) + body)

    msg_id, payload = read_frame(sock)
    code = struct.unpack("<i", payload[2:6])[0]
    print(f"frame1 id={msg_id} code={code}")
    if msg_id != 10001 or code != 1:
        raise SystemExit("SCAuth 不正确")

    msg_id, payload = read_frame(sock)
    print(f"frame2 id={msg_id} length={len(payload)}")
    if msg_id == 10003:
        print("OK: 收到 SCPlayerData(10003)")
    else:
        print("警告: 没有收到 SCPlayerData(10003)")