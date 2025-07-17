using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;

namespace ClassicUO.LegionScripting
{
    public static class PersistentVars
    {
        private const string DATABASE_FILE = "legionvars.sqlite";
        private const string GlobalScopeKey = "GLOBAL";

        private static string _charScopeKey = "";
        private static string _accountScopeKey = "";
        private static string _serverScopeKey = "";

        private static readonly object _dbLock = new();
        private static readonly ConcurrentQueue<(API.PersistentVar scope, string scopeKey, string key, string value)> _saveQueue = new();
        private static int _saveTaskRunning = 0;

        private static string DbPath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", DATABASE_FILE);
        private static IntPtr _connection = IntPtr.Zero;

        // SQLite P/Invoke declarations
        private const string SQLITE_DLL = "sqlite3";
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr ppDb);
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_close(IntPtr db);
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_prepare_v2(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string zSql, int nByte, out IntPtr ppStmt, IntPtr pzTail);
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_step(IntPtr stmt);
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_finalize(IntPtr stmt);
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_bind_text(IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPStr)] string value, int length, IntPtr destructor);
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_column_text(IntPtr stmt, int col);
        
        [DllImport(SQLITE_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_errmsg(IntPtr db);

        // SQLite constants
        private const int SQLITE_OK = 0;
        private const int SQLITE_ROW = 100;
        private const int SQLITE_DONE = 101;
        private static readonly IntPtr SQLITE_TRANSIENT = new IntPtr(-1);

        public static void Load()
        {
            _charScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username + ProfileManager.CurrentProfile.CharacterName;
            _accountScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username;
            _serverScopeKey = ProfileManager.CurrentProfile.ServerName;
            Connect();
        }

        private static void Connect()
        {
            if (_connection == IntPtr.Zero)
            {
                // Ensure the Data directory exists
                var dataDir = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                var result = sqlite3_open(DbPath, out _connection);
                if (result != SQLITE_OK)
                {
                    throw new Exception($"Failed to open SQLite database: {result}");
                }

                // Set WAL mode
                ExecuteNonQuery("PRAGMA journal_mode = WAL;");

                // Create table
                ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS PersistentVars (
                        Scope TEXT NOT NULL,
                        ScopeKey TEXT NOT NULL,
                        Key TEXT NOT NULL,
                        Value TEXT,
                        PRIMARY KEY (Scope, ScopeKey, Key)
                    );");
            }
        }

        private static void ExecuteNonQuery(string sql)
        {
            var result = sqlite3_prepare_v2(_connection, sql, -1, out var stmt, IntPtr.Zero);
            if (result != SQLITE_OK)
            {
                var errorMsg = GetErrorMessage();
                throw new Exception($"Failed to prepare statement: {result} - {errorMsg}");
            }

            try
            {
                result = sqlite3_step(stmt);
                if (result != SQLITE_DONE && result != SQLITE_ROW)
                {
                    var errorMsg = GetErrorMessage();
                    throw new Exception($"Failed to execute statement: {result} - {errorMsg}");
                }
            }
            finally
            {
                sqlite3_finalize(stmt);
            }
        }

        private static string GetErrorMessage()
        {
            var ptr = sqlite3_errmsg(_connection);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : "Unknown error";
        }

        private static (API.PersistentVar scope, string scopeKey) GetScopeKeyPair(API.PersistentVar scope) =>
            scope switch
            {
                API.PersistentVar.Char => (scope, _charScopeKey),
                API.PersistentVar.Account => (scope, _accountScopeKey),
                API.PersistentVar.Server => (scope, _serverScopeKey),
                API.PersistentVar.Global => (scope, GlobalScopeKey),
                _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
            };

        public static string GetVar(API.PersistentVar scope, string key, string defaultValue = "")
        {
            var (s, scopeKey) = GetScopeKeyPair(scope);

            lock (_dbLock)
            {
                Connect();

                const string sql = @"SELECT Value FROM PersistentVars WHERE Scope = ? AND ScopeKey = ? AND Key = ?;";
                var result = sqlite3_prepare_v2(_connection, sql, -1, out var stmt, IntPtr.Zero);
                if (result != SQLITE_OK)
                {
                    var errorMsg = GetErrorMessage();
                    throw new Exception($"Failed to prepare statement: {result} - {errorMsg}");
                }

                try
                {
                    // Bind parameters
                    sqlite3_bind_text(stmt, 1, s.ToString(), -1, SQLITE_TRANSIENT);
                    sqlite3_bind_text(stmt, 2, scopeKey, -1, SQLITE_TRANSIENT);
                    sqlite3_bind_text(stmt, 3, key, -1, SQLITE_TRANSIENT);

                    result = sqlite3_step(stmt);
                    if (result == SQLITE_ROW)
                    {
                        var valuePtr = sqlite3_column_text(stmt, 0);
                        if (valuePtr != IntPtr.Zero)
                        {
                            return Marshal.PtrToStringAnsi(valuePtr) ?? defaultValue;
                        }
                    }

                    return defaultValue;
                }
                finally
                {
                    sqlite3_finalize(stmt);
                }
            }
        }

        public static void SaveVar(API.PersistentVar scope, string key, string value)
        {
            var (s, scopeKey) = GetScopeKeyPair(scope);

            _saveQueue.Enqueue((s, scopeKey, key, value));

            // Only start the save task if not already running
            if (Interlocked.CompareExchange(ref _saveTaskRunning, 1, 0) == 0)
            {
                _ = Task.Run(ProcessSaveQueue);
            }
        }

        public static void DeleteVar(API.PersistentVar scope, string key)
        {
            var (s, scopeKey) = GetScopeKeyPair(scope);

            _saveQueue.Enqueue((s, scopeKey, key, null)); // null value = delete

            if (Interlocked.CompareExchange(ref _saveTaskRunning, 1, 0) == 0)
            {
                _ = Task.Run(ProcessSaveQueue);
            }
        }

        private static async Task ProcessSaveQueue()
        {
            try
            {
                while (_saveQueue.TryDequeue(out var item))
                {
                    lock (_dbLock)
                    {
                        Connect();

                        void Exec((API.PersistentVar scope, string scopeKey, string key, string value) i)
                        {
                            IntPtr stmt = IntPtr.Zero;
                            try
                            {
                                int result;
                                if (i.value == null)
                                {
                                    // Delete operation
                                    const string deleteSql = @"DELETE FROM PersistentVars WHERE Scope = ? AND ScopeKey = ? AND Key = ?;";
                                    result = sqlite3_prepare_v2(_connection, deleteSql, -1, out stmt, IntPtr.Zero);
                                    if (result != SQLITE_OK)
                                    {
                                        var errorMsg = GetErrorMessage();
                                        throw new Exception($"Failed to prepare delete statement: {result} - {errorMsg}");
                                    }

                                    sqlite3_bind_text(stmt, 1, i.scope.ToString(), -1, SQLITE_TRANSIENT);
                                    sqlite3_bind_text(stmt, 2, i.scopeKey, -1, SQLITE_TRANSIENT);
                                    sqlite3_bind_text(stmt, 3, i.key, -1, SQLITE_TRANSIENT);
                                }
                                else
                                {
                                    // Insert or replace operation
                                    const string insertSql = @"INSERT OR REPLACE INTO PersistentVars (Scope, ScopeKey, Key, Value) VALUES (?, ?, ?, ?);";
                                    result = sqlite3_prepare_v2(_connection, insertSql, -1, out stmt, IntPtr.Zero);
                                    if (result != SQLITE_OK)
                                    {
                                        var errorMsg = GetErrorMessage();
                                        throw new Exception($"Failed to prepare insert statement: {result} - {errorMsg}");
                                    }

                                    sqlite3_bind_text(stmt, 1, i.scope.ToString(), -1, SQLITE_TRANSIENT);
                                    sqlite3_bind_text(stmt, 2, i.scopeKey, -1, SQLITE_TRANSIENT);
                                    sqlite3_bind_text(stmt, 3, i.key, -1, SQLITE_TRANSIENT);
                                    sqlite3_bind_text(stmt, 4, i.value ?? "", -1, SQLITE_TRANSIENT);
                                }

                                result = sqlite3_step(stmt);
                                if (result != SQLITE_DONE)
                                {
                                    var errorMsg = GetErrorMessage();
                                    throw new Exception($"Failed to execute statement: {result} - {errorMsg}");
                                }
                            }
                            finally
                            {
                                if (stmt != IntPtr.Zero)
                                {
                                    sqlite3_finalize(stmt);
                                }
                            }
                        }

                        Exec(item);

                        while (_saveQueue.TryDequeue(out var nextItem))
                        {
                            Exec(nextItem);
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _saveTaskRunning, 0);
            }
        }

        public static void Unload()
        {
            if (_saveQueue.Count > 0)
            {
                if (Interlocked.CompareExchange(ref _saveTaskRunning, 1, 0) == 0)
                {
                    Task.Run(ProcessSaveQueue).Wait();
                }
            }

            // Close the database connection
            lock (_dbLock)
            {
                if (_connection != IntPtr.Zero)
                {
                    sqlite3_close(_connection);
                    _connection = IntPtr.Zero;
                }
            }
        }
    }
}