using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using RonCafeApp.Models;

namespace RonCafeApp.Services;

public class DatabaseService
{
    private readonly string _dbPath;
    private const string CONNECTION_STRING = "Data Source={0};Cache=Shared";

    public DatabaseService()
    {
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RonCafeApp");
        Directory.CreateDirectory(appDataDir);
        _dbPath = Path.Combine(appDataDir, "RonCafeLauncher.db");
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS LauncherConfig (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                BackgroundColor TEXT NOT NULL DEFAULT '#1E1E2E',
                SidebarColor TEXT NOT NULL DEFAULT '#181825',
                AccentColor TEXT NOT NULL DEFAULT '#89B4FA',
                UseCoverArtView INTEGER NOT NULL DEFAULT 0,
                LastModified DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS Apps (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Category TEXT NOT NULL,
                ExecutionPath TEXT NOT NULL,
                IconPath TEXT NOT NULL DEFAULT '/Assets/placeholder.png',
                CoverArtPath TEXT NOT NULL DEFAULT '/Assets/placeholder.png',
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_apps_category ON Apps(Category);

            CREATE TABLE IF NOT EXISTS RunningProcesses (
                ProcessId INTEGER PRIMARY KEY,
                AppId INTEGER NOT NULL,
                Category TEXT NOT NULL,
                StartedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (AppId) REFERENCES Apps(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_running_category ON RunningProcesses(Category);

            INSERT OR IGNORE INTO LauncherConfig (Id) VALUES (1);
        ";

        command.ExecuteNonQuery();
        AddWallpaperColumn();
        connection.Close();
    }

    public bool HasExistingData()
    {
        try
        {
            using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Apps";

            var result = (long?)command.ExecuteScalar() ?? 0;
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    // ─── Config Methods ──────────────────────────────────────────────────────
    public LauncherConfig LoadConfig()
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT BackgroundColor, SidebarColor, AccentColor, UseCoverArtView FROM LauncherConfig WHERE Id = 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new LauncherConfig
            {
                BackgroundColor = reader.GetString(0),
                SidebarColor = reader.GetString(1),
                AccentColor = reader.GetString(2),
                UseCoverArtReview = reader.GetInt32(3) == 1,
                Apps = LoadApps()
            };
        }

        return new LauncherConfig { Apps = new List<AppItem>() };
    }

    public void SaveConfig(LauncherConfig config)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE LauncherConfig 
                SET BackgroundColor = @bgColor, 
                    SidebarColor = @sidebarColor, 
                    AccentColor = @accentColor,
                    UseCoverArtView = @useCoverArt,
                    LastModified = CURRENT_TIMESTAMP
                WHERE Id = 1";

            command.Parameters.AddWithValue("@bgColor", config.BackgroundColor ?? "#1E1E2E");
            command.Parameters.AddWithValue("@sidebarColor", config.SidebarColor ?? "#181825");
            command.Parameters.AddWithValue("@accentColor", config.AccentColor ?? "#89B4FA");
            command.Parameters.AddWithValue("@useCoverArt", config.UseCoverArtReview ? 1 : 0);

            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"Failed to save config: {ex.Message}");
            throw;
        }
    }

    // ─── App Methods ─────────────────────────────────────────────────────────
    public List<AppItem> LoadApps()
    {
        var apps = new List<AppItem>();

        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Category, ExecutionPath, IconPath, CoverArtPath 
            FROM Apps 
            ORDER BY Category, Name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            apps.Add(new AppItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                getExecutionPATH = reader.GetString(3),
                IconPlaceholder = reader.GetString(4),
                CoverArtPlaceholder = reader.GetString(5)
            });
        }

        return apps;
    }

    public int AddApp(AppItem app)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Apps (Name, Category, ExecutionPath, IconPath, CoverArtPath)
            VALUES (@name, @category, @execPath, @iconPath, @coverPath);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@name", app.Name);
        command.Parameters.AddWithValue("@category", app.Category);
        command.Parameters.AddWithValue("@execPath", app.getExecutionPATH);
        command.Parameters.AddWithValue("@iconPath", app.IconPlaceholder);
        command.Parameters.AddWithValue("@coverPath", app.CoverArtPlaceholder);

        try
        {
            return (int)(long)command.ExecuteScalar()!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding app: {ex.Message}");
            throw;
        }
    }

    public void UpdateApp(AppItem app)
    {
        if (app.Id == 0)
            throw new InvalidOperationException("Cannot update app without Id");

        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Apps 
            SET Name = @name, 
                Category = @category, 
                ExecutionPath = @execPath,
                IconPath = @iconPath,
                CoverArtPath = @coverPath,
                UpdatedAt = CURRENT_TIMESTAMP
            WHERE Id = @id";

        command.Parameters.AddWithValue("@id", app.Id);
        command.Parameters.AddWithValue("@name", app.Name);
        command.Parameters.AddWithValue("@category", app.Category);
        command.Parameters.AddWithValue("@execPath", app.getExecutionPATH);
        command.Parameters.AddWithValue("@iconPath", app.IconPlaceholder);
        command.Parameters.AddWithValue("@coverPath", app.CoverArtPlaceholder);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating app: {ex.Message}");
            throw;
        }
    }

    public void DeleteApp(int appId)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Apps WHERE Id = @id";
        command.Parameters.AddWithValue("@id", appId);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting app: {ex.Message}");
            throw;
        }
    }

    public AppItem? GetAppById(int appId)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Category, ExecutionPath, IconPath, CoverArtPath 
            FROM Apps 
            WHERE Id = @id";

        command.Parameters.AddWithValue("@id", appId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new AppItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                getExecutionPATH = reader.GetString(3),
                IconPlaceholder = reader.GetString(4),
                CoverArtPlaceholder = reader.GetString(5)
            };
        }

        return null;
    }

    // ─── Running Process Tracking ────────────────────────────────────────────
    public void LogProcessStart(int appId, int processId, string category)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO RunningProcesses (ProcessId, AppId, Category)
            VALUES (@pid, @appId, @category)";

        command.Parameters.AddWithValue("@pid", processId);
        command.Parameters.AddWithValue("@appId", appId);
        command.Parameters.AddWithValue("@category", category);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging process start: {ex.Message}");
        }
    }

    public void LogProcessEnd(int processId)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM RunningProcesses WHERE ProcessId = @pid";
        command.Parameters.AddWithValue("@pid", processId);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging process end: {ex.Message}");
        }
    }

    public List<(int ProcessId, string Category)> GetRunningGameProcesses()
    {
        var processes = new List<(int, string)>();

        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT ProcessId, Category FROM RunningProcesses WHERE Category = 'Games'";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            processes.Add((reader.GetInt32(0), reader.GetString(1)));
        }

        return processes;
    }

    public int GetRunningProcessCount(string category = "")
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(category))
        {
            command.CommandText = "SELECT COUNT(*) FROM RunningProcesses";
        }
        else
        {
            command.CommandText = "SELECT COUNT(*) FROM RunningProcesses WHERE Category = @category";
            command.Parameters.AddWithValue("@category", category);
        }

        return (int)(long)command.ExecuteScalar()!;
    }
    
    private void AddWallpaperColumn()
    {
        try
        {
            using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
            connection.Open();
        
            var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE LauncherConfig ADD COLUMN WallpaperPath TEXT";
            command.ExecuteNonQuery();
        
            Console.WriteLine("Added WallpaperPath column to LauncherConfig table");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
        {
            // Column already exists, that's fine
            Console.WriteLine("WallpaperPath column already exists");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding wallpaper column: {ex.Message}");
        }
    }
}
