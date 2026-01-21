#!/bin/bash
# Database Reset Script untuk DonutErp

echo "?? DonutErp Database Reset Tool"
echo "================================"

# Check if we're in the right directory
if [ ! -f "DonutErp.sln" ]; then
    echo "? Error: Run this script from the root directory (where DonutErp.sln is)"
    exit 1
fi

echo ""
echo "?? Step 1: Delete old database..."
rm -f "*.db" 2>/dev/null || true
echo "? Old database deleted"

echo ""
echo "?? Step 2: Remove old migrations..."
cd DonutErp.Infrastructure
rm -rf "Migrations" 2>/dev/null || true
cd ..
echo "? Old migrations removed"

echo ""
echo "?? Step 3: Create initial migration..."
dotnet ef migrations add "InitialCreate" -p DonutErp.Infrastructure -s DonutErp.UI --verbose

if [ $? -ne 0 ]; then
    echo "? Migration creation failed!"
    exit 1
fi
echo "? Migration created"

echo ""
echo "?? Step 4: Apply migrations to database..."
dotnet ef database update -p DonutErp.Infrastructure -s DonutErp.UI --verbose

if [ $? -ne 0 ]; then
    echo "? Database update failed!"
    exit 1
fi
echo "? Database updated"

echo ""
echo "?? Step 5: Verify database..."
if [ -f "DonutErp.UI/donuterp.db" ]; then
    echo "? Database created successfully at DonutErp.UI/donuterp.db"
    ls -lh "DonutErp.UI/donuterp.db"
else
    echo "? Database file not found!"
    exit 1
fi

echo ""
echo "? ALL DONE! Database is ready to use."
echo ""
echo "Next steps:"
echo "1. Open the application in Visual Studio"
echo "2. The database will be seeded with sample data on first run"
echo "3. Everything should work now!"
