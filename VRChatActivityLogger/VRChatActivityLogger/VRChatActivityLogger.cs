﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VRChatActivityToolsShared.Database;
using static System.Environment;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;

namespace VRChatActivityLogger
{
    /// <summary>
    /// VRChatでの活動履歴をログから取得し、データベースに保存するクラス
    /// </summary>
    class VRChatActivityLogger
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly NLog.Logger logger = Logger.GetLogger();

        /// <summary>
        /// VRChatのログの保存場所
        /// 規定値はローカルマシンのフォルダ
        /// </summary>
        public string VRChatLogFilePath { get; set; } =
            Path.Combine(Regex.Replace(GetFolderPath(SpecialFolder.LocalApplicationData), @"\\[^\\]+$", ""), "LocalLow", "VRChat", "VRChat");

        /// <summary>
        /// DB接続設定
        /// </summary>
        private readonly DbConfig _configuration;

        public VRChatActivityLogger(DbConfig configuration)
        {
            _configuration = configuration;
            var pgm = Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory);
            errorFilePath = Path.Combine(pgm, "Logs", "VRChatActivityLogger", "errorfile.txt");
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        /// <returns></returns>
        public int Run()
        {
            var logger = Logger.GetLogger();
            try
            {
                logger.Info($"エラーログ出力先: {errorFilePath}");
                ClearErrorInfoFile();

                var factory = new DbOperatorFactory(_configuration);

                // ログ解析
                var activityLogs = new List<ActivityLog>();
                foreach (var file in Directory.EnumerateFiles(VRChatLogFilePath, "output_log_*"))
                {
                    logger.Debug("ログを解析中 " + file);
                    activityLogs.AddRange(ParseVRChatLog(file));
                }
                activityLogs = activityLogs.OrderBy(a => a.Timestamp).ToList();

                // DB確認
                using (var context = factory.GetDbContext())
                {
                    if (!context.Database.CanConnect())
                    {
                        logger.Info("データベースが見つかりませんでした。新しく作成します。");

                        factory.DbMigration.CreateDbAndTables(context);

                        logger.Info("データベースを作成しました。");
                    }

                    // DBバージョンチェック
                    var currentVersion = factory.DbMigration.GetCurrentVersion(context);

                    if (currentVersion < ActivityContextBase.Version)
                    {
                        logger.Info("古いバージョンのデータベースを使用しています。データベースのアップグレードを行います。");

                        factory.DbMigration.UpgradeDatabase(context);

                        logger.Info("データベースをアップグレードしました。");
                    }
                    else if (ActivityContextBase.Version < currentVersion)
                    {
                        throw new InvalidOperationException("新しいバージョンのアプリで作成されたデータベースが存在するため、処理を中断します。");
                    }
                }

                // DB更新
                using (var db = factory.GetDbContext())
                {
                    var lastActivity = db.ActivityLogs.Find(db.ActivityLogs.Max(a => a.ID));
                    if (lastActivity != null)
                    {
                        var idBackup = lastActivity.ID;
                        lastActivity.ID = null;
                        for (int i = 0; i < activityLogs.Count; i++)
                        {
                            if (activityLogs[i].Timestamp == lastActivity.Timestamp)
                            {
                                if (activityLogs[i].Equals(lastActivity))
                                {
                                    activityLogs.RemoveRange(0, i + 1);
                                    break;
                                }
                            }
                        }
                        lastActivity.ID = idBackup;
                    }

                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            db.AddRange(activityLogs);
                            db.SaveChanges();
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }

                    }

                }

                logger.Info(activityLogs.Count + "件追加しました。");

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                WriteErrorInfoFile();
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// VRChatのログを解析します。
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<ActivityLog> ParseVRChatLog(string filePath)
        {
            string rawData = "";
            var activityLogs = new List<ActivityLog>();
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.GetEncoding("UTF-8")))
            {
                processingFilePath = filePath;
                processingLineNumber = 0;

                while ((rawData = sr.ReadLine()) != null)
                {
                    processingLineNumber++;

                    if (rawData.Length > 25 && rawData.Substring(20, 5) == "Error")
                    {
                        continue;
                    }

                    Match match = RegexPatterns.All.Match(rawData);

                    if (!match.Success)
                    {
                        continue;
                    }

                    processingLine = match.Value;

                    if (match.Groups[PatternType.ReceivedInvite].Value.Length != 0)
                    {
                        var m = RegexPatterns.ReceivedInviteDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.ReceivedInvite,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            NotificationID = m.Groups[4].Value,
                            UserID = m.Groups[3].Value,
                            UserName = m.Groups[2].Value,
                            WorldID = m.Groups[5].Value,
                            WorldName = m.Groups[6].Value,
                        };

                        if (m.Groups[8].Success)
                        {
                            activityLog.Message = m.Groups[8].Value;
                        }

                        if (m.Groups[10].Success)
                        {
                            activityLog.Url = m.Groups[10].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.ReceivedRequestInvite].Value.Length != 0)
                    {
                        var m = RegexPatterns.ReceivedRequestInviteDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.ReceivedRequestInvite,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            NotificationID = m.Groups[4].Value,
                            UserID = m.Groups[3].Value,
                            UserName = m.Groups[2].Value,
                        };

                        if (m.Groups[6].Success)
                        {
                            activityLog.Message = m.Groups[6].Value;
                        }

                        if (m.Groups[8].Success)
                        {
                            activityLog.Url = m.Groups[8].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.SendInvite].Value.Length != 0)
                    {
                        var m = RegexPatterns.SendInviteDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.SendInvite,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            UserID = m.Groups[2].Value,
                            WorldID = m.Groups[3].Value,
                            WorldName = m.Groups[4].Value,
                        };

                        if (m.Groups[6].Success)
                        {
                            activityLog.Message = m.Groups[6].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.SendRequestInvite].Value.Length != 0)
                    {
                        var m = RegexPatterns.SendRequestInviteDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.SendRequestInvite,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            UserID = m.Groups[2].Value,
                        };

                        if (m.Groups[3].Success)
                        {
                            activityLog.Message = m.Groups[3].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.JoinedRoom1].Value.Length != 0)
                    {
                        var m = RegexPatterns.JoinedRoom1Detail.Match(match.ToString());
                        activityLogs.Add(new ActivityLog
                        {
                            ActivityType = ActivityType.JoinedRoom,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            WorldID = m.Groups[3].Value,
                        });
                    }
                    else if (match.Groups[PatternType.JoinedRoom2].Value.Length != 0)
                    {
                        var m = RegexPatterns.JoinedRoom2Detail.Match(match.ToString());
                        if (activityLogs.Any() && activityLogs[activityLogs.Count - 1].ActivityType == ActivityType.JoinedRoom)
                        {
                            activityLogs[activityLogs.Count - 1].WorldName = m.Groups[3].Value;
                        }
                        else
                        {
                            activityLogs.Add(new ActivityLog
                            {
                                ActivityType = ActivityType.JoinedRoom,
                                Timestamp = DateTime.Parse(m.Groups[1].Value),
                                WorldName = m.Groups[3].Value,
                            });
                        }
                    }
                    else if (match.Groups[PatternType.MetPlayer].Value.Length != 0)
                    {
                        var m = RegexPatterns.MetPlayerDetail.Match(match.ToString());
                        activityLogs.Add(new ActivityLog
                        {
                            ActivityType = ActivityType.MetPlayer,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            UserName = m.Groups[3].Value,
                        });
                    }
                    else if (match.Groups[PatternType.SendFriendRequest].Value.Length != 0)
                    {
                        var m = RegexPatterns.SendFriendRequestDetail.Match(match.ToString());
                        activityLogs.Add(new ActivityLog
                        {
                            ActivityType = ActivityType.SendFriendRequest,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            UserID = m.Groups[2].Value,
                        });
                    }
                    else if (match.Groups[PatternType.ReceivedFriendRequest].Value.Length != 0)
                    {
                        var m = RegexPatterns.ReceivedFriendRequestDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.ReceivedFriendRequest,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            NotificationID = m.Groups[4].Value,
                            UserID = m.Groups[3].Value,
                            UserName = m.Groups[2].Value,
                        };

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.AcceptFriendRequest].Value.Length != 0)
                    {
                        var m = RegexPatterns.AcceptFriendRequestDetail.Match(match.ToString());
                        activityLogs.Add(new ActivityLog
                        {
                            ActivityType = ActivityType.AcceptFriendRequest,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            UserName = m.Groups[2].Value,
                            UserID = m.Groups[3].Value,
                            NotificationID = m.Groups[4].Value,
                        });
                    }
                    else if (match.Groups[PatternType.ReceivedInviteResponse].Value.Length != 0)
                    {
                        var m = RegexPatterns.ReceivedInviteResponseDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.ReceivedInviteResponse,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            NotificationID = m.Groups[4].Value,
                            UserName = m.Groups[2].Value,
                            UserID = m.Groups[3].Value,
                        };

                        if (m.Groups[6].Success)
                        {
                            activityLog.Message = m.Groups[6].Value;
                        }

                        if (m.Groups[8].Success)
                        {
                            activityLog.Url = m.Groups[8].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.ReceivedRequestInviteResponse].Value.Length != 0)
                    {
                        var m = RegexPatterns.ReceivedRequestInviteResponseDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.ReceivedRequestInviteResponse,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            NotificationID = m.Groups[4].Value,
                            UserName = m.Groups[2].Value,
                            UserID = m.Groups[3].Value,
                        };

                        if (m.Groups[6].Success)
                        {
                            activityLog.Message = m.Groups[6].Value;
                        }

                        if (m.Groups[8].Success)
                        {
                            activityLog.Url = m.Groups[8].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.PlayedVideo1].Value.Length != 0)
                    {
                        // VRCSDK2で作成したワールドの場合

                        var m = RegexPatterns.PlayedVideo1Detail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.PlayedVideo,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            UserName = m.Groups[2].Value,
                            Url = m.Groups[3].Value,
                        };

                        // 1回のアクションにつき2行のログが出力される事がある
                        if (activityLogs[activityLogs.Count - 1] == activityLog)
                        {
                            continue;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.PlayedVideo2].Value.Length != 0)
                    {
                        // VRCSDK3で作成したワールドの場合

                        var m = RegexPatterns.PlayedVideo2Detail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.PlayedVideo,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            Url = m.Groups[2].Value,
                        };

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.AcceptInvite].Value.Length != 0)
                    {
                        var m = RegexPatterns.AcceptInviteDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.AcceptInvite,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            NotificationID = m.Groups[4].Value,
                            UserID = m.Groups[3].Value,
                            UserName = m.Groups[2].Value,
                            WorldID = m.Groups[5].Value,
                            WorldName = m.Groups[6].Value,
                        };

                        if (m.Groups[8].Success)
                        {
                            activityLog.Message = m.Groups[8].Value;
                        }

                        if (m.Groups[10].Success)
                        {
                            activityLog.Url = m.Groups[10].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else if (match.Groups[PatternType.AcceptRequestInvite].Value.Length != 0)
                    {
                        var m = RegexPatterns.AcceptRequestInviteDetail.Match(match.ToString());
                        var activityLog = new ActivityLog
                        {
                            ActivityType = ActivityType.AcceptRequestInvite,
                            Timestamp = DateTime.Parse(m.Groups[1].Value),
                            NotificationID = m.Groups[4].Value,
                            UserID = m.Groups[3].Value,
                            UserName = m.Groups[2].Value,
                        };

                        if (m.Groups[6].Success)
                        {
                            activityLog.Message = m.Groups[6].Value;
                        }

                        if (m.Groups[8].Success)
                        {
                            activityLog.Url = m.Groups[8].Value;
                        }

                        activityLogs.Add(activityLog);
                    }
                    else
                    {
                        continue;
                    }

                    processingLine = string.Empty;
                }

                processingLineNumber = 0;
                processingFilePath = string.Empty;
            }

            return activityLogs;
        }

        private string processingFilePath = string.Empty;

        private int processingLineNumber = 0;

        private string processingLine = string.Empty;

        private readonly string errorFilePath;

        /// <summary>
        /// エラーファイルをクリアします。
        /// </summary>
        private void ClearErrorInfoFile()
        {
            if (File.Exists(errorFilePath))
            {
                File.Delete(errorFilePath);
            }
        }

        /// <summary>
        /// エラーファイルを書き出します。
        /// </summary>
        private void WriteErrorInfoFile()
        {
            if (!string.IsNullOrEmpty(processingFilePath))
            {
                var body =
                    Path.GetFullPath(processingFilePath) + Environment.NewLine +
                    processingLineNumber + Environment.NewLine +
                    processingLine + Environment.NewLine;
                File.WriteAllText(errorFilePath, body);

                logger.Error($"{processingFilePath}#{processingLineNumber}");
                logger.Error($"{processingLine}");
            }
        }
    }
}
