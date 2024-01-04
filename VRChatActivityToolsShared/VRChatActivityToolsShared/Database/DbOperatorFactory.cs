﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace VRChatActivityToolsShared.Database
{
    public class DbOperatorFactory
    {
        /// <summary>
        /// コンテキスト
        /// </summary>
        private ActivityContextBase _context;

        /// <summary>
        /// バージョン移行オブジェクト
        /// </summary>
        public IDbMigration DbMigration { get; private set; }

        /// <summary>
        /// 接続設定オブジェクト
        /// </summary>
        private readonly DbConfig _config;

        public DbOperatorFactory(DbConfig config)
        {
            _config = config;

            switch (_config.DbKind)
            {
                case DbKind.SQLite:
                    var c = ActivityContextSQLite.ParseConnectionString(config.ConnectionString);
                    DbMigration = new DbMigrationSQLite(c);
                    break;
                case DbKind.MariaDB:
                    DbMigration = new DbMigrationMariaDb(config.ConnectionString);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// コンテキストを返す
        /// Disposeされていれば新しくインスタンスを返す
        /// </summary>
        /// <returns></returns>
        public ActivityContextBase GetDbContext()
        {
            if (_context?.Disposed != false)
            {
                var connectionString = _config.ConnectionString;
                var builder = new DbContextOptionsBuilder<ActivityContextBase>();
                //コンテキストがnullかDisposeされている場合
                switch (_config.DbKind)
                {
                    case DbKind.SQLite:
                        builder.UseSqlite(connectionString);
                        _context = new ActivityContextSQLite(builder.Options);
                        break;
                    case DbKind.MariaDB:
                        builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                        _context = new ActivityContextMariaDb(builder.Options);
                        break;
                    default:
                        break;
                }
            }

            return _context;
        }
    }
}
