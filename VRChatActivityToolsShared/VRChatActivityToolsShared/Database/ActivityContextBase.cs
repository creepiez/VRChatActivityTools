using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRChatActivityToolsShared.Database
{
    public abstract class ActivityContextBase : DbContext
    {
        /// <summary>
        /// データベースのバージョン
        /// </summary>
        public const int Version = 3;

        /// <summary>
        /// ActivityLogsテーブル
        /// </summary>
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        /// <summary>
        /// Informationテーブル
        /// </summary>
        public DbSet<Information> Information { get; set; }

        /// <summary>
        /// Dispose()がコールされた後か？
        /// </summary>
        public bool Disposed { get; protected set; }

        /// <summary>
        /// DBにテーブルがあるか確認する
        /// </summary>
        /// <param name="tableName">確認するテーブル名</param>
        /// <returns></returns>
        public abstract bool HasTable(string tableName);

        public ActivityContextBase(DbContextOptions<ActivityContextBase> options) : base(options) { }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (Disposed)
            { return; }
            base.Dispose();
            Disposed = true;
        }
    }
}
