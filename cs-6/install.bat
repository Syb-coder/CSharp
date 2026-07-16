@echo off
title Smart Hotel Management System - Setup
chcp 65001 >nul
cd /d "%~dp0"

echo ========================================
echo   HotelSys (cs-6) - Environment Setup
echo ========================================
echo.

REM --- Step 1: Check .NET 8 Desktop Runtime ---
echo [1/3] Checking .NET 8 Desktop Runtime...

where dotnet >nul 2>nul
if errorlevel 1 (
    echo   [X] dotnet command not found
    echo   Please install .NET 8 Desktop Runtime:
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    goto :fail
)

dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 8." >nul
if errorlevel 1 (
    echo   [X] .NET 8 Desktop Runtime not detected
    echo   Download: https://dotnet.microsoft.com/download/dotnet/8.0
    goto :fail
)
echo   [OK] .NET 8 Desktop Runtime installed

REM --- Step 2: Check SQL Server ---
echo.
echo [2/3] Checking SQL Server...

set "SQL_OK=0"
sc query MSSQLSERVER 2>nul | findstr /C:"RUNNING" >nul
if not errorlevel 1 set "SQL_OK=1"

if "%SQL_OK%"=="0" (
    sc query MSSQL$SQLEXPRESS 2>nul | findstr /C:"RUNNING" >nul
    if not errorlevel 1 set "SQL_OK=1"
)

if "%SQL_OK%"=="0" (
    echo   [X] No running SQL Server service detected
    echo   Please install SQL Server Express:
    echo   https://www.microsoft.com/sql-server/sql-server-downloads
    goto :fail
)
echo   [OK] SQL Server is running

REM --- Step 3: Run database fix script ---
echo.
echo [3/3] Running database fix script...
echo.

powershell -ExecutionPolicy Bypass -NoProfile -File ".\fix-db.ps1"
if errorlevel 1 (
    echo.
    echo [FAIL] Database initialization failed.
    echo   Please check the error messages above.
    goto :fail
)

echo.
echo ========================================
echo   Setup Complete! Login accounts:
echo     Admin  : admin   / admin123
echo     Front  : front01 / front123
echo ========================================
echo.
goto :end

:fail
echo.
echo ========================================
echo   Setup incomplete. Fix issues and retry.
echo ========================================
echo.

:end
pause