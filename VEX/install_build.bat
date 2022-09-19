@echo off
SET DLL_SOURCE="..\VEX\bin\Debug\VEX.dll"
SET MOD_DEST="K:\Games\steamapps\common\Valheim\BepInEx\plugins"
echo Copying build from
echo %DLL_SOURCE%
echo to
echo %MOD_DEST%
copy %DLL_SOURCE% %MOD_DEST%
PAUSE