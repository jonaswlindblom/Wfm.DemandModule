@echo off
setlocal

title Frontend UI Dev Server

set "ROOT=%~dp0"
set "FRONTEND=%ROOT%frontend.ui"

if not exist "%FRONTEND%\package.json" (
    echo [FEL] Hittar inte frontend.ui\package.json
    echo Kontrollera att bat-filen ligger i solution-roten.
    pause
    exit /b 1
)

cd /d "%FRONTEND%"
echo Startar frontend i %CD%
call npm run dev -- --open

if errorlevel 1 (
    echo.
    echo [FEL] Kunde inte starta frontend.
    pause
    exit /b 1
)

endlocal