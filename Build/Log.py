import os
import sys
import time

log_file = 'D:/HelloWorld/Build/build.log'

if __name__ == '__main__':
    with open(log_file, 'r', encoding='utf-8') as f:
        data = f.read()
        print(data)