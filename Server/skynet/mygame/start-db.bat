@echo off
rem ---------------------------------------------------------------
rem  启动本地 MySQL + Redis（开发用便携版）
rem  MySQL: D:\HelloWorld\Server\mysql        数据目录 D:\HelloWorld\Server\mysql\data   127.0.0.1:3306
rem  Redis: D:\HelloWorld\Server\redis                                        127.0.0.1:6379
rem  初始化数据库：D:\HelloWorld\Server\mysql\bin\mysql.exe -u root --skip-password < mygame\sql\init.sql
rem ---------------------------------------------------------------
cd /d %~dp0\..

if not exist D:\HelloWorld\Server\mysql\data (
  echo [mysql] 初始化数据目录...
  D:\HelloWorld\Server\mysql\bin\mysqld.exe --initialize-insecure --basedir=D:\HelloWorld\Server\mysql --datadir=D:\HelloWorld\Server\mysql\data
)

start "redis" /MIN D:\HelloWorld\Server\redis\redis-server.exe --port 6379 --bind 127.0.0.1 --dir D:\HelloWorld\Server\redis
start "mysql" /MIN D:\HelloWorld\Server\mysql\bin\mysqld.exe --basedir=D:\HelloWorld\Server\mysql --datadir=D:\HelloWorld\Server\mysql\data --port=3306 --bind-address=127.0.0.1 --mysqlx=0 --character-set-server=utf8mb4 --collation-server=utf8mb4_general_ci --log-error=D:\HelloWorld\Server\mysql\data\error.log

echo 已启动（端口 3306 / 6379），首次使用请执行 mygame\sql\init.sql