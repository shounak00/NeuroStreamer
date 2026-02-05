@echo off
echo ========================================
echo Neuro-Streamer Build Helper
echo ========================================
echo.

echo Step 1: Building C++ DLL...
cd NativeDLL
if not exist build mkdir build
cd build
cmake ..
cmake --build . --config Release
cd ../..

echo.
echo Step 2: Copying DLL to Unity...
copy /Y "NativeDLL\build\Release\MedicalImageProcessing.dll" "UnityProject\Assets\Plugins\x86_64\"

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Next steps:
echo 1. Open Unity project
echo 2. Verify DLL in Assets/Plugins/x86_64/
echo 3. Press Play to test
echo.
pause
