@echo off
title ENY Kapi Kontrol v2.1.0 - Derleme
echo ============================================
echo   ENY Kapi Kontrol v2.1.0 (SDK Destegi) - Derleniyor...
echo ============================================
echo.

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set REFERENCES=/reference:"System.dll" /reference:"System.Windows.Forms.dll" /reference:"System.Drawing.dll" /reference:"System.Core.dll" /reference:"System.Data.dll" /reference:"System.Runtime.Serialization.dll"
set SOURCES="DoorControl.cs" "HikSdk.cs"

echo [1/3] Kaynak kod derleniyor (x64)...
%CSC% /target:winexe /platform:x64 /optimize %REFERENCES% /main:ENY_Kapi_Kontrol.Program /out:"ENY_Kapi_Kontrol.exe" %SOURCES%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [HATA] Derleme basarisiz!
    pause
    exit /b 1
)

echo.
echo Guncelleme yardimcisi derleniyor...
%CSC% /target:winexe /platform:x64 /optimize /reference:"System.dll" /reference:"System.Windows.Forms.dll" /reference:"System.Core.dll" /reference:"System.IO.Compression.dll" /out:"ENYUpdater.exe" "Updater.cs"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [HATA] Guncelleme yardimcisi derlenemedi!
    pause
    exit /b 1
)

echo.
echo [2/3] Basarili! Cikti: ENY_Kapi_Kontrol.exe
for %%I in (ENY_Kapi_Kontrol.exe) do echo Boyut: %%~zI bytes

echo.
echo [3/3] SDK DLL dosyalari ana klasore kopyalaniyor...
if not exist "sdk\HCNetSDK.dll" (
    echo [UYARI] sdk\HCNetSDK.dll bulunamadi! SDK destegi olmadan calisir.
) else (
    copy /y "sdk\*.dll"             "." >nul 2>nul
    if not exist "HCNetSDKCom" mkdir "HCNetSDKCom"
    copy /y "sdk\HCNetSDKCom\*.dll" "HCNetSDKCom\" >nul 2>nul
    echo SDK dosyalari kopyalandi.
)

echo.
echo Baslatmak icin: ENY_Kapi_Kontrol.exe
echo HCNetSDK.dll ve HCCore.dll ayni klasorde olmali!
echo.
pause
