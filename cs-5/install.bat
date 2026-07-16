@echo off
cd /d "%~dp0"
echo ========================================
echo   Library Management System - DB Setup
echo ========================================
echo.
echo Running database fix script...
echo.

powershell -ExecutionPolicy Bypass -NoProfile -File ".\fix-db.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [FAIL] Database setup failed. Check errors above.
    echo.
) else (
    echo.
    echo [OK] Database setup complete. You can now run the program.
    echo.
)

pause