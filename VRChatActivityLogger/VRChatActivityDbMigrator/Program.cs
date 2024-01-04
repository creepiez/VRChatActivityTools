using Microsoft.Extensions.Configuration;
using VRChatActivityToolsShared.Database;

namespace VRChatActivityDbMigrator
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine($"VRChatActivityTools DB移行ツール");
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? string.Empty;
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                   .AddJsonFile("appsettings.json")
                                                   .AddJsonFile($"appsettings.{env}.json", true)
                                                   .Build();

            var src = new DbConfig(config.GetSection("Source"));
            var srcFactory = new DbOperatorFactory(src);

            var dst = new DbConfig(config.GetSection("Destination"));
            var dstFactory = new DbOperatorFactory(dst);

            Console.WriteLine($"移行元: {src.DbKind} {src.ConnectionString}");
            Console.WriteLine("↓");
            Console.WriteLine($"移行先: {dst.DbKind} {dst.ConnectionString}");

            using var srcContext = srcFactory.GetDbContext();
            using var dstContext = dstFactory.GetDbContext();
            //元DBの存在を確認
            if (!srcContext.Database.CanConnect())
            {
                //元が無ければエラーで終了
                Console.WriteLine("移行元DBに接続出来ないため処理を中止します。");
                return -1;
            }
            //元のバージョンが最新版か確認
            if (srcFactory.DbMigration.GetCurrentVersion(srcContext) != ActivityContextBase.Version)
            {
                //古いバージョンなのでアップグレードする
                Console.WriteLine("移行元のバージョンをアップグレードしています。");
                srcFactory.DbMigration.UpgradeDatabase(srcContext);
            }

            var isDstCreatedNew = false;
            //先DBの存在を確認
            if (!dstContext.Database.CanConnect())
            {
                //先が無ければ作る
                Console.WriteLine("移行先のデータベーススキーマがありません。新規作成しています。");
                dstFactory.DbMigration.CreateDbAndTables(dstContext);
                isDstCreatedNew = true;
            }

            //先の情報テーブルの存在を確認
            if (dstContext.HasTable(nameof(ActivityContextBase.Information)))
            {
                //先の情報があるか確認
                if (!dstContext.Information.Any())
                {
                    //（メイン処理）バージョン情報を移動
                    Console.WriteLine("バージョン情報を移行しています。");
                    dstContext.Add(srcContext.Information.First());
                    dstContext.SaveChanges();
                }
                else if (dstContext.Information.First()?.Version != srcContext.Information.First()?.Version)
                {
                    //先の情報が異なっているのでエラーで終了
                    Console.WriteLine("移行元と移行先のバージョン情報が異なるため処理を中止します。");
                    return -1;
                }
                else if(!isDstCreatedNew)
                {
                    //先にすでにバージョン情報がある
                    Console.WriteLine("移行先にバージョン情報があるため、移行済みの可能性があります。");
                    Console.WriteLine("移行処理を続行しますか？(Y/n)");
                    while (true)
                    {
                        var key = Console.ReadKey();
                        if (key.Key == ConsoleKey.Y)
                        { break; }
                        else if (key.Key == ConsoleKey.N)
                        {
                            Console.WriteLine("処理を中止しました。");
                            return -1;
                        }
                        else
                        { Console.WriteLine("異なるキーが入力されました。再入力してください。"); }
                    }
                }
            }
            else
            {
                //情報テーブルを作る
            }

            //先のログテーブルの存在を確認
            if (dstContext.HasTable(nameof(ActivityContextBase.ActivityLogs)))
            {
                //先にログが存在するか確認
                if (dstContext.ActivityLogs.Any())
                {
                    //存在していればエラーで終了
                    Console.WriteLine("移行先にログデータが既に存在するため処理を中止します。");
                    return -1;
                }
            }
            else
            {
                //ログテーブルを作る
            }

            //（メイン処理）ログ移動
            Console.WriteLine("ログレコードを移行しています。");
            Console.WriteLine($"ログレコード数: {srcContext.ActivityLogs.Count()}行");
            dstContext.AddRange(srcContext.ActivityLogs);
            dstContext.SaveChanges();

            Console.WriteLine("移行が完了しました。");
            return 1;
        }
    }
}