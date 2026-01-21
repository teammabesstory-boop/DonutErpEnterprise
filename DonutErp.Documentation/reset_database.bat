@echo off
REM Database Reset Script untuk DonutErp (Windows)
REM Run this from the root directory where DonutErp.sln is located

echo.
echo ?? DonutErp Database Reset Tool
echo ================================
echo.

REM Check if we're in the right directory
if not exist "DonutErp.sln" (
    echo ? Error: Run this script from the root directory (where DonutErp.sln is)
    pause
    exit /b 1
)

echo ?? Step 1: Delete old database files...
cd DonutErp.UI
del /f /q "donuterp.db" 2>nul
del /f /q "donuterp.db-shm" 2>nul
del /f /q "donuterp.db-wal" 2>nul
cd ..
echo ? Old database files deleted

echo.
echo ?? Step 2: Remove old migrations...
rmdir /s /q "DonutErp.Infrastructure\Migrations" 2>nul
echo ? Old migrations removed

echo.
echo ?? Step 3: Create initial migration...
dotnet ef migrations add "InitialCreate" -p DonutErp.Infrastructure -s DonutErp.UI --verbose
if %ERRORLEVEL% neq 0 (
    echo ? Migration creation failed!
    pause
    exit /b 1
)
echo ? Migration created

echo.
echo ?? Step 4: Apply migrations to database...
dotnet ef database update -p DonutErp.Infrastructure -s DonutErp.UI --verbose
if %ERRORLEVEL% neq 0 (
    echo ? Database update failed!
    pause
    exit /b 1
)
echo ? Database updated

echo.
echo ?? Step 5: Verify database...
if exist "DonutErp.UI\donuterp.db" (
    echo ? Database created successfully at DonutErp.UI\donuterp.db
    dir "DonutErp.UI\donuterp.db"
) else (
    echo ? Database file not found!
    pause
    exit /b 1
)

echo.
echo ? ALL DONE! Database is ready to use.
echo.
echo Next steps:
echo 1. Open the application in Visual Studio
echo 2. The database will be seeded with sample data on first run
echo 3. Everything should work now!
echo.
pause
