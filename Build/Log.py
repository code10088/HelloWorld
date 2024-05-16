import os
import sys
import time

log_file = 'D:/HelloWorld/Build/build.log'

if os.path.exists(log_file):
    with open(log_file, 'r', encoding='utf-8', errors='ignore') as f:
        data = f.read()
        print(data)