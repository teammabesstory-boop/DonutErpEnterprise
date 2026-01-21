# ?? DonutErp - Database & Troubleshooting Guide

## ?? Current Issue: SQLite Schema Error

### Error Message
```
SQLite Error 1: 'no such column: i.Category'
```

### Root Cause
The SQLite database schema doesn't match the Entity Framework model. This happens when:
- Migrations haven't been applied
- Database file is outdated
- Entity Framework hasn't created the tables

### ? Solution: Reset Database

#### **Option 1: Windows (Easiest)**
Double-click: `DonutErp.Documentation\reset_database.bat`

This will:
1. ? Delete old database files
2. ? Remove outdated migrations
3. ? Create fresh migrations
4. ? Apply to database
5. ? Verify success

#### **Option 2: Windows PowerShell / Linux Bash**
```bash
chmod +x DonutErp.Documentation/reset_database.sh
./DonutErp.Documentation/reset_database.sh
```

#### **Option 3: Manual Command Line**

```bash
# 1. Delete old database
cd DonutErp.UI
del donuterp.db
cd ..

# 2. Remove old migrations
rmdir /s DonutErp.Infrastructure\Migrations

# 3. Create fresh migration
dotnet ef migrations add "InitialCreate" -p DonutErp.Infrastructure -s DonutErp.UI

# 4. Apply to database
dotnet ef database update -p DonutErp.Infrastructure -s DonutErp.UI

# 5. Verify
dir DonutErp.UI\donuterp.db
```

---

## ?? Troubleshooting Guide

### Problem 1: "no such column: X"
**Cause**: Database schema is outdated or doesn't exist

**Solution**:
```bash
# Delete database and recreate it
rm DonutErp.UI/donuterp.db
dotnet ef database update -p DonutErp.Infrastructure -s DonutErp.UI
```

### Problem 2: "SQLite database is locked"
**Cause**: Another process has the database open

**Solution**:
1. Close Visual Studio
2. Close any running instances of DonutErp
3. Delete: `donuterp.db`, `donuterp.db-shm`, `donuterp.db-wal`
4. Reopen Visual Studio
5. Run the app (database will be recreated)

### Problem 3: "A DbCommand was canceled"
**Cause**: Database operation timeout

**Solution**:
```csharp
// In App.xaml.cs ServiceInitializer:
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=donuterp.db;Command Timeout=30;");
});
```

### Problem 4: "One or more validation errors were detected during model generation"
**Cause**: Entity model conflicts

**Solution**:
```bash
# Clean build
dotnet clean
dotnet build
```

### Problem 5: "Migration XX already exists"
**Cause**: Duplicate migration files

**Solution**:
```bash
# Remove last migration
dotnet ef migrations remove -p DonutErp.Infrastructure -s DonutErp.UI

# Create new one
dotnet ef migrations add "FixedMigration" -p DonutErp.Infrastructure -s DonutErp.UI
```

---

## ?? Database Maintenance

### Backup Database
```bash
# Create backup before making changes
copy DonutErp.UI\donuterp.db DonutErp.UI\donuterp.db.backup
```

### Reset to Backup
```bash
# Restore from backup
copy DonutErp.UI\donuterp.db.backup DonutErp.UI\donuterp.db
```

### View Database Structure
```bash
# Using sqlite3 command line
sqlite3 DonutErp.UI/donuterp.db ".schema Ingredients"
```

### Export Data
```bash
# Backup data to CSV
sqlite3 DonutErp.UI/donuterp.db \
  ".mode csv" \
  ".output ingredients.csv" \
  "SELECT * FROM Ingredients;"
```

---

## ? Verification Checklist

After resetting database, verify:

- [ ] `donuterp.db` file exists in `DonutErp.UI` folder
- [ ] Build succeeds: `dotnet build`
- [ ] App starts without database errors
- [ ] Can navigate to Inventory page
- [ ] Can see ingredient list
- [ ] Can navigate to Production page
- [ ] Can navigate to Finance page
- [ ] No "no such column" errors

---

## ?? Common Commands

### Create Migration
```bash
dotnet ef migrations add "DescriptiveName" -p DonutErp.Infrastructure -s DonutErp.UI
```

### Apply Migrations
```bash
dotnet ef database update -p DonutErp.Infrastructure -s DonutErp.UI
```

### Remove Last Migration
```bash
dotnet ef migrations remove -p DonutErp.Infrastructure -s DonutErp.UI
```

### View All Migrations
```bash
dotnet ef migrations list -p DonutErp.Infrastructure -s DonutErp.UI
```

### Generate SQL Script
```bash
dotnet ef migrations script -p DonutErp.Infrastructure -s DonutErp.UI -o migration.sql
```

### Revert to Previous State
```bash
dotnet ef database update "PreviousMigrationName" -p DonutErp.Infrastructure -s DonutErp.UI
```

---

## ?? Debugging Tips

### Enable EF Core Logging
Add to `ServiceInitializer.cs`:
```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=donuterp.db");
    
    #if DEBUG
    options.LogTo(
        Console.WriteLine,
        Microsoft.Extensions.Logging.LogLevel.Information);
    #endif
});
```

### Check Database Corruption
```bash
# Integrity check
sqlite3 DonutErp.UI/donuterp.db "PRAGMA integrity_check;"
```

### Optimize Database
```bash
# Vacuum and optimize
sqlite3 DonutErp.UI/donuterp.db "VACUUM;"
```

---

## ?? Additional Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [DonutErp Integration Guide](./IntegrationGuide.cs)
- [DonutErp Data Model](./DataModelSummary.cs)

---

## ?? Still Having Issues?

1. **Delete everything and start fresh**:
   ```bash
   # Complete reset
   rm -rf DonutErp.Infrastructure/Migrations
   rm DonutErp.UI/donuterp.db*
   dotnet clean
   dotnet build
   dotnet ef database update
   ```

2. **Check file permissions**: Ensure `DonutErp.UI` folder is writable

3. **Update .NET SDK**: Ensure you have .NET 10 installed
   ```bash
   dotnet --version
   ```

4. **Check Git status**: Ensure no file conflicts
   ```bash
   git status
   ```

---

**If issues persist, delete the database and let it recreate on first run!** ?
