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

    // ─── Database Initialization (Only creates tables if they don't exist) ──
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
                WallpaperPath TEXT,
                LastModified DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS Apps (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Category TEXT NOT NULL,
                ExecutionPath TEXT NOT NULL,
                IconPath TEXT NOT NULL DEFAULT '/Assets/placeholder.png',
                CoverArtPath TEXT NOT NULL DEFAULT '/Assets/placeholder.png',
                LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
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
        connection.Close();
    }

    // ─── READ-ONLY Config Methods ──────────────────────────────────────────
    public LauncherConfig LoadConfig()
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT BackgroundColor, SidebarColor, AccentColor, UseCoverArtView, WallpaperPath FROM LauncherConfig WHERE Id = 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new LauncherConfig
            {
                BackgroundColor = reader.GetString(0),
                SidebarColor = reader.GetString(1),
                AccentColor = reader.GetString(2),
                UseCoverArtReview = reader.GetInt32(3) == 1,
                WallpaperPath = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Apps = LoadApps()
            };
        }

        return new LauncherConfig { Apps = new List<AppItem>() };
    }

    // ─── READ-ONLY App Methods ─────────────────────────────────────────────
    public List<AppItem> LoadApps()
    {
        var apps = new List<AppItem>();

        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Category, ExecutionPath, IconPath, CoverArtPath, LastModified, CreatedAt, UpdatedAt
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
                CoverArtPlaceholder = reader.GetString(5),
                LastModified = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                CreatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            });
        }

        return apps;
    }

    public AppItem? GetAppById(int appId)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Category, ExecutionPath, IconPath, CoverArtPath, LastModified, CreatedAt, UpdatedAt
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
                CoverArtPlaceholder = reader.GetString(5),
                LastModified = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                CreatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            };
        }

        return null;
    }

    public AppItem? GetAppByName(string name)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Category, ExecutionPath, IconPath, CoverArtPath, LastModified, CreatedAt, UpdatedAt
            FROM Apps 
            WHERE Name = @name";

        command.Parameters.AddWithValue("@name", name);

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
                CoverArtPlaceholder = reader.GetString(5),
                LastModified = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                CreatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            };
        }

        return null;
    }

    public List<AppItem> GetAppsByCategory(string category)
    {
        var apps = new List<AppItem>();

        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Category, ExecutionPath, IconPath, CoverArtPath, LastModified, CreatedAt, UpdatedAt
            FROM Apps 
            WHERE Category = @category
            ORDER BY Name";

        command.Parameters.AddWithValue("@category", category);

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
                CoverArtPlaceholder = reader.GetString(5),
                LastModified = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                CreatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            });
        }

        return apps;
    }

    public int GetAppCount()
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Apps";

        return (int)(long)command.ExecuteScalar()!;
    }

    public int GetAppCountByCategory(string category)
    {
        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Apps WHERE Category = @category";
        command.Parameters.AddWithValue("@category", category);

        return (int)(long)command.ExecuteScalar()!;
    }

    public List<string> GetAllCategories()
    {
        var categories = new List<string>();

        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT Category FROM Apps ORDER BY Category";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(reader.GetString(0));
        }

        return categories;
    }

    // ─── Running Process Tracking (Needed for Curfew) ──────────────────────
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

    public List<(int ProcessId, int AppId, string Category, DateTime StartedAt)> GetAllRunningProcesses()
    {
        var processes = new List<(int, int, string, DateTime)>();

        using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT ProcessId, AppId, Category, StartedAt FROM RunningProcesses";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            processes.Add((
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDateTime(3)
            ));
        }

        return processes;
    }

    // ─── Health Check ─────────────────────────────────────────────────────────
    public bool IsDatabaseAccessible()
    {
        try
        {
            using var connection = new SqliteConnection(string.Format(CONNECTION_STRING, _dbPath));
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.ExecuteScalar();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetDatabasePath() => _dbPath;

    public long GetDatabaseSize()
    {
        try
        {
            var fileInfo = new FileInfo(_dbPath);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    public DateTime? GetDatabaseLastModified()
    {
        try
        {
            var fileInfo = new FileInfo(_dbPath);
            return fileInfo.Exists ? fileInfo.LastWriteTime : null;
        }
        catch
        {
            return null;
        }
    }
}