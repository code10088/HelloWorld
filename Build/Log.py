import os
import sys
import time

log_file = 'D:/HelloWorld/Build/build.log'

if os.path.exists(log_file):
    with open(log_file, 'r', encoding='gbk', errors='ignore') as file:
        print(file.read())