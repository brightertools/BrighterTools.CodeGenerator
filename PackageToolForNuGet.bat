@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%BrighterTools.CodeGenerator.csproj"
set "OUTPUT_DIR=%SCRIPT_DIR%artifacts\nuget"
set "CONFIGURATION=Release"
set "VERSION=%~1"

if not exist "%PROJECT%" (
    echo Project file not found: %PROJECT%
    exit /b 1
)

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)

echo Restoring BrighterTools.CodeGenerator...
dotnet restore "%PROJECT%"
if errorlevel 1 exit /b %errorlevel%

echo Building BrighterTools.CodeGenerator...
dotnet build "%PROJECT%" -c %CONFIGURATION% --no-restore
if errorlevel 1 exit /b %errorlevel%

echo Packing BrighterTools.CodeGenerator tool package...
if "%VERSION%"=="" (
    dotnet pack "%PROJECT%" -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%"
) else (
    dotnet pack "%PROJECT%" -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:Version=%VERSION%
)
if errorlevel 1 exit /b %errorlevel%

echo.
echo Package output:
echo   %OUTPUT_DIR%
echo.
echo Publish command:
if "%VERSION%"=="" (
    echo   dotnet nuget push "%OUTPUT_DIR%\BrighterTools.CodeGenerator.*.nupkg" --source https://api.nuget.org/v3/index.json --api-key ^<API_KEY^>
) else (
    echo   dotnet nuget push "%OUTPUT_DIR%\BrighterTools.CodeGenerator.%VERSION%.nupkg" --source https://api.nuget.org/v3/index.json --api-key ^<API_KEY^>
)

endlocal
