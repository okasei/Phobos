using System.IO;
using Phobos.Class.Plugin.BuiltIn;
using Microsoft.Data.Sqlite;
using Phobos.Interface.Database;
using Phobos.Utils.Arcusrix;
using System.Data;

namespace Phobos.Class.Database
{
    /// <summary>
    /// SQLite 数据库连接实现
    /// </summary>
    public class PCSqliteDatabase : PIDatabase, IDisposable
    {
        private SqliteConnection? _connection;
        private SqliteTransaction? _transaction;
        private readonly string _databasePath;
        private readonly bool _useEncryption;
        private readonly string _password;
        private bool _disposed = false;

        public string DatabasePath => _databasePath;
        public bool IsConnected => _connection?.State == ConnectionState.Open;

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="databasePath">数据库文件路径</param>
        /// <param name="password">密码（如需加密，需使用SQLCipher版本）</param>
        /// <param name="useEncryption">是否使用加密（需要SQLCipher支持）</param>
        public PCSqliteDatabase(string databasePath, string? password = null, bool useEncryption = false)
        {
            _databasePath = databasePath;
            _useEncryption = useEncryption;
            _password = password ?? PUMachineCode.GetMachineCode();
        }

        public async Task<bool> Connect(string? password = null)
        {
            try
            {
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var connectionStringBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                };

                // 仅在启用加密且使用 SQLCipher 时设置密码
                // 注意：标准 Microsoft.Data.Sqlite 不支持加密
                // 如需加密，请使用 Microsoft.Data.Sqlite.Core + SQLitePCLRaw.bundle_e_sqlcipher
                if (_useEncryption)
                {
                    connectionStringBuilder.Password = password ?? _password;
                }

                _connection = new SqliteConnection(connectionStringBuilder.ToString());
                await _connection.OpenAsync();

                // 初始化表结构
                await InitializeTables();

                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Database.Connect", $"Database connection failed: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeTables()
        {
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS Phobos_Main (
                    Key TEXT PRIMARY KEY NOT NULL,
                    Content TEXT NOT NULL DEFAULT '',
                    UpdateUID TEXT NOT NULL DEFAULT '',
                    UpdateTime TEXT NOT NULL DEFAULT (datetime('now')),
                    LastValue TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS Phobos_Plugin (
                    PackageName TEXT PRIMARY KEY NOT NULL,
                    Name TEXT NOT NULL DEFAULT '',
                    Manufacturer TEXT NOT NULL DEFAULT '',
                    Description TEXT NOT NULL DEFAULT '',
                    Version TEXT NOT NULL DEFAULT '1.0.0',
                    Secret TEXT NOT NULL DEFAULT '',
                    Language TEXT NOT NULL DEFAULT 'en-US',
                    InstallTime TEXT NOT NULL DEFAULT (datetime('now')),
                    Directory TEXT NOT NULL DEFAULT '',
                    Icon TEXT NOT NULL DEFAULT '',
                    IsSystemPlugin INTEGER NOT NULL DEFAULT 0,
                    SettingUri TEXT NOT NULL DEFAULT '',
                    UninstallInfo TEXT NOT NULL DEFAULT '',
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    UpdateTime TEXT NOT NULL DEFAULT (datetime('now')),
                    Entry TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS Phobos_Appdata (
                    UKey TEXT PRIMARY KEY NOT NULL,
                    PackageName TEXT NOT NULL DEFAULT '',
                    Content TEXT NOT NULL DEFAULT '',
                    UpdateUID TEXT NOT NULL DEFAULT '',
                    UpdateTime TEXT NOT NULL DEFAULT (datetime('now')),
                    LastValue TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS Phobos_Protocol (
                    UUID TEXT PRIMARY KEY NOT NULL,
                    Protocol TEXT NOT NULL DEFAULT '',
                    AssociatedItem TEXT NOT NULL DEFAULT '',
                    UpdateUID TEXT NOT NULL DEFAULT '',
                    UpdateTime TEXT NOT NULL DEFAULT (datetime('now')),
                    LastValue TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS Phobos_AssociatedItem (
                    Name TEXT PRIMARY KEY NOT NULL,
                    PackageName TEXT NOT NULL DEFAULT '',
                    Description TEXT NOT NULL DEFAULT '',
                    Command TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS Phobos_Boot (
                    UUID TEXT PRIMARY KEY NOT NULL,
                    Command TEXT NOT NULL DEFAULT '',
                    PackageName TEXT NOT NULL DEFAULT '',
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    Priority INTEGER NOT NULL DEFAULT 100
                );

                CREATE TABLE IF NOT EXISTS Phobos_Shell (
                    Protocol TEXT PRIMARY KEY NOT NULL,
                    AssociatedItem TEXT NOT NULL DEFAULT '',
                    UpdateUID TEXT NOT NULL DEFAULT '',
                    UpdateTime TEXT NOT NULL DEFAULT (datetime('now')),
                    LastValue TEXT NOT NULL DEFAULT ''
                );

                CREATE INDEX IF NOT EXISTS idx_appdata_package ON Phobos_Appdata(PackageName);
                CREATE INDEX IF NOT EXISTS idx_associated_package ON Phobos_AssociatedItem(PackageName);
                CREATE INDEX IF NOT EXISTS idx_protocol_protocol ON Phobos_Protocol(Protocol);
                CREATE INDEX IF NOT EXISTS idx_boot_package ON Phobos_Boot(PackageName);
                CREATE INDEX IF NOT EXISTS idx_boot_priority ON Phobos_Boot(Priority);
                CREATE INDEX IF NOT EXISTS idx_plugin_system ON Phobos_Plugin(IsSystemPlugin);
                CREATE INDEX IF NOT EXISTS idx_plugin_enabled ON Phobos_Plugin(IsEnabled);
            ";

            await ExecuteNonQuery(createTablesSql);
        }

        public async Task Disconnect()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task<int> ExecuteNonQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            if (_connection == null || !IsConnected)
                throw new InvalidOperationException("Database is not connected");

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _transaction;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            if (_connection == null || !IsConnected)
                throw new InvalidOperationException("Database is not connected");

            var results = new List<Dictionary<string, object>>();

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _transaction;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null! : value;
                }
                results.Add(row);
            }

            return results;
        }

        public async Task<object?> ExecuteScalar(string sql, Dictionary<string, object>? parameters = null)
        {
            if (_connection == null || !IsConnected)
                throw new InvalidOperationException("Database is not connected");

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _transaction;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : result;
        }

        public async Task BeginTransaction()
        {
            if (_connection == null || !IsConnected)
                throw new InvalidOperationException("Database is not connected");

            _transaction = (SqliteTransaction)await _connection.BeginTransactionAsync();
        }

        public async Task CommitTransaction()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransaction()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}