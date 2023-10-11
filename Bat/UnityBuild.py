import os
import sys
import time

unity_exe = 'C:/Program Files/Unity2021.3.25/Editor/Unity.exe'
project_path = 'D:/HelloWorld/HelloWorld'
log_file = os.getcwd() + '/HelloWorld/build.log'

def call_unity_static_func(func):
    os.system('taskkill /IM Unity.exe /F')
    if os.path.exists(log_file):
        os.remove(log_file)
    time.sleep(1)
    cmd = 'start %s -quit -batchmode -projectPath %s -logFile %s -executeMethod %s --version:%s'%(unity_exe,project_path,log_file,func,sys.argv[1])
    os.system(cmd)
  
def monitor_unity_log(target_log):
    while not os.path.exists(log_file):
        time.sleep(0.1)
    fd = open(log_file, 'r', encoding='utf-8')
    fd.seek(pos, 0)
    while True:
        line = fd.readline()
        if target_log in line:
            print(line)
            break
    fd.close()
 
if __name__ == '__main__':
    call_unity_static_func('BuildEditor.BuildPlayer')
    #monitor_unity_log('Finish')
