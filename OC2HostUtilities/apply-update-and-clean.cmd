@echo off
setlocal
set "PROCESS=Overcooked2.exe"
set "INSTALL_ROOT=/Users/bulubulubulu/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/plugins/OC2HostUtilities"
set "LEGACY_ROOT=/Users/bulubulubulu/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/plugins/CHA"
set "LEGACY_CLIENT_UTILITIES_ROOT=/Users/bulubulubulu/Library/Application Support/Steam/steamapps/common/Overcooked! 2/BepInEx/plugins/ClientUtilities"
set "KEEP_NAME=v1.9.1"
set "VERSION_DIR=%INSTALL_ROOT%\%KEEP_NAME%"
set "VERSION_PENDING=%INSTALL_ROOT%\%KEEP_NAME%.pending.tmp"
set "OLD_VERSION_COUNT=0"
set "LOG_FILE=%INSTALL_ROOT%\apply-update-and-clean.log"
echo [%date% %time%] watcher started, keep=%KEEP_NAME%>"%LOG_FILE%"
echo [%date% %time%] install root: %INSTALL_ROOT%>>"%LOG_FILE%"
echo [%date% %time%] legacy root: %LEGACY_ROOT%>>"%LOG_FILE%"
echo [%date% %time%] legacy ClientUtilities root: %LEGACY_CLIENT_UTILITIES_ROOT%>>"%LOG_FILE%"
echo [%date% %time%] version dir: %VERSION_DIR%>>"%LOG_FILE%"
echo [%date% %time%] version pending: %VERSION_PENDING%>>"%LOG_FILE%"
echo [%date% %time%] old version count: %OLD_VERSION_COUNT%>>"%LOG_FILE%"
echo [%date% %time%] mode: wait for natural game exit>>"%LOG_FILE%"
echo [%date% %time%] launch after apply: true>>"%LOG_FILE%"
:wait_game
tasklist /FI "IMAGENAME eq %PROCESS%" 2>NUL | find /I "%PROCESS%" >NUL
if not errorlevel 1 (
  echo [%date% %time%] waiting %PROCESS%>>"%LOG_FILE%"
  timeout /t 1 /nobreak >NUL
  goto wait_game
)
echo [%date% %time%] %PROCESS% exited>>"%LOG_FILE%"
echo [%date% %time%] waiting 1 second after game exit>>"%LOG_FILE%"
timeout /t 1 /nobreak >NUL
echo [%date% %time%] applying pending version directory>>"%LOG_FILE%"
call :apply_pending_version
if errorlevel 1 (
  echo [%date% %time%] pending version apply failed>>"%LOG_FILE%"
) else (
  echo [%date% %time%] pending version apply finished>>"%LOG_FILE%"
)
echo [%date% %time%] applying resource update>>"%LOG_FILE%"
call :apply_resources
if errorlevel 1 (
  echo [%date% %time%] resource update failed>>"%LOG_FILE%"
) else (
  echo [%date% %time%] resource update finished>>"%LOG_FILE%"
)
call :delete_legacy_root
if errorlevel 1 (
  echo [%date% %time%] legacy cleanup failed>>"%LOG_FILE%"
) else (
  echo [%date% %time%] legacy cleanup finished>>"%LOG_FILE%"
)
call :delete_legacy_client_utilities_root
if errorlevel 1 (
  echo [%date% %time%] legacy ClientUtilities cleanup failed>>"%LOG_FILE%"
) else (
  echo [%date% %time%] legacy ClientUtilities cleanup finished>>"%LOG_FILE%"
)
call :delete_legacy_files
if errorlevel 1 (
  echo [%date% %time%] legacy file cleanup failed>>"%LOG_FILE%"
) else (
  echo [%date% %time%] legacy file cleanup finished>>"%LOG_FILE%"
)
call :delete_old_version_dirs
if errorlevel 1 (
  echo [%date% %time%] old version cleanup failed>>"%LOG_FILE%"
) else (
  echo [%date% %time%] old version cleanup finished>>"%LOG_FILE%"
)
echo [%date% %time%] waiting 1 seconds before launch>>"%LOG_FILE%"
timeout /t 1 /nobreak >NUL
echo [%date% %time%] launching game>>"%LOG_FILE%"
start "" "steam://launch/728880"
echo [%date% %time%] launch command sent>>"%LOG_FILE%"
del /q "%~f0" >NUL 2>NUL
exit /b
:apply_pending_version
if not exist "%VERSION_PENDING%" (
  echo [%date% %time%] no pending version dir, skip version switch>>"%LOG_FILE%"
  exit /b 0
)
echo [%date% %time%] pending version dir exists>>"%LOG_FILE%"
if exist "%VERSION_DIR%" (
  echo [%date% %time%] deleting current same-version dir before overwrite>>"%LOG_FILE%"
  rd /s /q "%VERSION_DIR%" >NUL 2>NUL
  if exist "%VERSION_DIR%" (
    echo [%date% %time%] failed to delete current same-version dir>>"%LOG_FILE%"
    exit /b 1
  )
) else (
  echo [%date% %time%] current version dir does not exist>>"%LOG_FILE%"
)
echo [%date% %time%] moving pending version into place>>"%LOG_FILE%"
move /y "%VERSION_PENDING%" "%VERSION_DIR%" >NUL
if errorlevel 1 (
  echo [%date% %time%] failed to move pending version into place>>"%LOG_FILE%"
  exit /b 1
)
echo [%date% %time%] pending version applied>>"%LOG_FILE%"
exit /b 0
:apply_resources
set "RESOURCE_ZIP=%INSTALL_ROOT%\HostUtilities.Resources.zip.download"
set "RESOURCE_MANIFEST=%INSTALL_ROOT%\HostUtilities.Resources.manifest.download"
set "RESOURCE_TMP=%INSTALL_ROOT%\OC2HostUtilitiesResource.tmp"
set "RESOURCE_DIR=%INSTALL_ROOT%\OC2HostUtilitiesResource"
echo [%date% %time%] resource tmp: %RESOURCE_TMP%>>"%LOG_FILE%"
echo [%date% %time%] resource dir: %RESOURCE_DIR%>>"%LOG_FILE%"
if not exist "%RESOURCE_TMP%" (
  echo [%date% %time%] no pending resource tmp, skip resource update>>"%LOG_FILE%"
  exit /b 0
)
if exist "%RESOURCE_DIR%" (
  echo [%date% %time%] deleting old resource dir>>"%LOG_FILE%"
  rd /s /q "%RESOURCE_DIR%" >NUL 2>NUL
  if exist "%RESOURCE_DIR%" (
    echo [%date% %time%] failed to delete old resource dir>>"%LOG_FILE%"
    exit /b 1
  )
) else (
  echo [%date% %time%] old resource dir does not exist>>"%LOG_FILE%"
)
echo [%date% %time%] moving resource tmp into place>>"%LOG_FILE%"
move /y "%RESOURCE_TMP%" "%RESOURCE_DIR%" >NUL
if errorlevel 1 (
  echo [%date% %time%] failed to move resource tmp>>"%LOG_FILE%"
  exit /b 1
)
del /f /q "%RESOURCE_ZIP%" >NUL 2>NUL
del /f /q "%RESOURCE_MANIFEST%" >NUL 2>NUL
echo [%date% %time%] deleted resource download files if present>>"%LOG_FILE%"
exit /b 0
:delete_legacy_root
if not defined LEGACY_ROOT (
  echo [%date% %time%] legacy root is empty, skip legacy cleanup>>"%LOG_FILE%"
  exit /b 0
)
if not exist "%LEGACY_ROOT%" (
  echo [%date% %time%] legacy root does not exist, skip: %LEGACY_ROOT%>>"%LOG_FILE%"
  exit /b 0
)
echo [%date% %time%] deleting legacy root: %LEGACY_ROOT%>>"%LOG_FILE%"
rd /s /q "%LEGACY_ROOT%" >NUL 2>NUL
if exist "%LEGACY_ROOT%" (
  echo [%date% %time%] failed to delete legacy root>>"%LOG_FILE%"
  exit /b 1
)
echo [%date% %time%] legacy root deleted>>"%LOG_FILE%"
exit /b 0
:delete_legacy_client_utilities_root
if not defined LEGACY_CLIENT_UTILITIES_ROOT (
  echo [%date% %time%] legacy ClientUtilities root is empty, skip cleanup>>"%LOG_FILE%"
  exit /b 0
)
if not exist "%LEGACY_CLIENT_UTILITIES_ROOT%" (
  echo [%date% %time%] legacy ClientUtilities root does not exist, skip: %LEGACY_CLIENT_UTILITIES_ROOT%>>"%LOG_FILE%"
  exit /b 0
)
echo [%date% %time%] deleting legacy ClientUtilities root: %LEGACY_CLIENT_UTILITIES_ROOT%>>"%LOG_FILE%"
rd /s /q "%LEGACY_CLIENT_UTILITIES_ROOT%" >NUL 2>NUL
if exist "%LEGACY_CLIENT_UTILITIES_ROOT%" (
  echo [%date% %time%] failed to delete legacy ClientUtilities root>>"%LOG_FILE%"
  exit /b 1
)
echo [%date% %time%] legacy ClientUtilities root deleted>>"%LOG_FILE%"
exit /b 0
:delete_legacy_files
call :delete_file "%INSTALL_ROOT%\HostUtilities.install-manifest.txt"
call :delete_file "%INSTALL_ROOT%\HostUtilities.dll"
call :delete_file "%INSTALL_ROOT%\HostUtilities.pdb"
call :delete_dir "%INSTALL_ROOT%\ConfigurationManager"
if errorlevel 1 exit /b 1
call :delete_dir "%INSTALL_ROOT%\Audio"
if errorlevel 1 exit /b 1
call :delete_dir "%INSTALL_ROOT%\ClientUtilities"
if errorlevel 1 exit /b 1
exit /b 0
:delete_old_version_dirs
if "%OLD_VERSION_COUNT%"=="0" (
  echo [%date% %time%] no old version dir to delete>>"%LOG_FILE%"
  exit /b 0
)
exit /b 0
:delete_file
if not exist "%~1" (
  echo [%date% %time%] legacy file missing, skip: %~1>>"%LOG_FILE%"
  exit /b 0
)
echo [%date% %time%] deleting legacy file: %~1>>"%LOG_FILE%"
del /f /q "%~1" >NUL 2>NUL
if exist "%~1" echo [%date% %time%] failed to delete legacy file: %~1>>"%LOG_FILE%"
exit /b 0
:delete_dir
if not exist "%~1" (
  echo [%date% %time%] legacy dir missing, skip: %~1>>"%LOG_FILE%"
  exit /b 0
)
echo [%date% %time%] deleting legacy dir: %~1>>"%LOG_FILE%"
rd /s /q "%~1" >NUL 2>NUL
if exist "%~1" (
  echo [%date% %time%] failed to delete legacy dir: %~1>>"%LOG_FILE%"
  exit /b 1
)
exit /b 0
:delete_version_dir
if not exist "%~1" (
  echo [%date% %time%] old version dir missing, skip: %~1>>"%LOG_FILE%"
  exit /b 0
)
echo [%date% %time%] deleting old version dir: %~1>>"%LOG_FILE%"
rd /s /q "%~1" >NUL 2>NUL
if exist "%~1" (
  echo [%date% %time%] failed to delete old version dir: %~1>>"%LOG_FILE%"
  exit /b 1
)
exit /b 0
