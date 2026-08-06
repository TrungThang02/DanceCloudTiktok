@echo off
chcp 65001 > nul
title Cloud Disco - Khoi chay tat ca
echo ========================================================
echo               CLOUD DISCO LIVE GAME LAUNCHER
echo ========================================================
echo.

cd /d "%~dp0TikTokBridge"

if not exist node_modules (
    echo [1/3] Dang cai dat node_modules cho TikTokBridge...
    call npm install
)

echo [2/3] Dang khoi chay TikTokBridge Node Server...
start /b node server.js > nul 2>&1

timeout /t 2 /nobreak > nul

echo [3/3] Dang mo trang dieu khien va Game Unity...
start http://127.0.0.1:3000/control.html

cd /d "%~dp0"
start TikTokLiveGameUnity.exe

echo.
echo [CHUC MUNG] Cloud Disco da mo thanh cong!
