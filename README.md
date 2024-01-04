VRChatActivityTools
====

VRChatのログを解析し、joinしたワールドや会った人の履歴などをデータベースに保存します。

Analyzes VRChat logs and stores the history of worlds joined and people met in a database. (English documentation is available under the Japanese version)

# 日本語

## インストール

### ダウンロード

バイナリは用意していません。

自分でコンパイルしてください。

### インストール:

ビルドしたバイナリファイル一式を適当なフォルダに配置してください。

※Program Filesなどの書き込み制限のあるフォルダには置かないでください。

VRChatActivityLogger.exeは64bit Windows以外にも、.NETが動作する限り意味もなく他OSでも動作します。

VRChatActivityLogViewer.exeは64 bit Windows専用です。

## 使い方

.NET 6.0 SDKランタイムをインストールしてください。

これでビルドもバイナリの起動も可能となります。

ビルドしないときは何とかしてください。

### VRChatActivityLogger.exe
VRChatのログを解析し、活動履歴のデータベースを作成します。

動作のためには設定ファイル`appsettings.conf`を、プログラムと同じディレクトリに作成する必要があります。

ビルドすると自動的にファイルがコピーされるので、そちらをご利用ください。

説明は後述します。

既にデータベースが作成されている場合は追加登録されていきます。

タスクスケジューラなどで定期実行されるようにすると便利かもしれません。

コンソール画面を表示しないで実行したい場合は以下の起動オプションを使用してください。

Windows以外では本起動オプションは無効となります。

```
VRChatActivityLogger.exe -console false
```

### VRChatActivityLogViewer.exe
データベースの内容をGUIで表示します。

データベースをまだ作成していない場合は、先にVRChatActivityLogger.exeを実行してください。

画面上のLoggerボタンをクリックする事でも実行する事ができます。

### appsettings.json

記入例は下記です。
``` json
{
  "ConnectionStrings": {
    "VRChatActivityLog": "Server=localhost;Database=VRChatActivityLog;Username=usr;Password=pwd;"    
  },
  "DbKind": "MariaDB",

  "VRChat": {
    "LogFileDir": ""
  }
}
```

#### `.ConnectionStrings.VRChatActivityLog`
DBへの接続文字列です。

詳細は各EFCoreのドキュメントをご覧ください。

[MySQL .NET Connection String Options](https://mysqlconnector.net/connection-options/)

[Entity Framework Core > Microsoft.Data.Sqlite > Basic usage](https://github.com/dotnet/efcore?tab=readme-ov-file#basic-usage-1)

#### `.ConnectionStrings.DbKind`
使用するDBエンジンです。下記をお使いください。

- `"MariaDB"`
- `"SQLite"`

Note: MySQLでも`"MariaDB"`を指定してください。相違はEFCoreが自動調整します。

#### `.VRChat.LogFileDir`
VRChatのログディレクトリです。

空欄にすると自動的にローカルマシンのアプリケーションデータフォルダとなります。

出力先を変えている方だけご利用ください。

### VRChatActivityLog.db
SQLiteモードでVRChatActivityLogger.exeを実行すると作成されるデータベースファイルです。

中身はSQLite3のデータなので、他のアプリと連携したりもできると思います。

ただし、接続文字列の特性上本ファイルのファイル名は可変です。

### VRChatActivityDbMigrator.exe
DBエンジン間やDB間でレコードのコピーを行います。

詳細は[別ページ](VRChatActivityLogger/VRChatActivityDbMigrator/README.md)をご覧ください。

## 既知の問題

inviteの送信履歴などから送信先となるユーザ名を表示する事はできません。VRChatのログにユーザ名が記録されない為です。

ネットワーク上のDBへ接続出来るか確認が出来ません。EFCoreの`CanConnect()`が「DBが存在するかを返す」為です。

## ライセンス

このプログラムにはMITライセンスが適用されます。

# English

## Installation

### Download

Sorry, there is no binary files. Please build yourself.

### Installation

Build this repository and place output files in an appropriate folder.

Do not place it in a write-restricted folder such as Program Files.

VRChatActivityLogger.exe can run with any OS which the .NET supports.

VRChatActivityLogViewer.exe is for 64bit Windows only.

## How to use :

Please install .NET 6.0 SDK Runtime before use.

Now you can build and run the binary.

If you don't build, please make yourself.

### VRChatActivityLogger.exe  

Analyzes the VRChat log and creates a database of activity history.

If a database has already been created, it will be added to the database.

It may be useful to use a task scheduler or similar to run it periodically.

If you want to run without displaying the console screen, use the following startup option.

In except Windows, this option will be disabled.

```
VRChatActivityLogger.exe -console false
```

### VRChatActivityLogViewer.exe

Displays the contents of the database in GUI.

If you have not yet created a database, run VRChatActivityLogger.exe first.

You can also run it by clicking the Logger button on the screen.

### appsettings.json

The sample is below.

``` json
{
  "ConnectionStrings": {
    "VRChatActivityLog": "Server=localhost;Database=VRChatActivityLog;Username=usr;Password=pwd;"    
  },
  "DbKind": "MariaDB",

  "VRChat": {
    "LogFileDir": ""
  }
}
```

#### `.ConnectionStrings.VRChatActivityLog`

Connection string to your DB.

Details for see each EFCore documents.

[MySQL .NET Connection String Options](https://mysqlconnector.net/connection-options/)

[Entity Framework Core > Microsoft.Data.Sqlite > Basic usage](https://github.com/dotnet/efcore?tab=readme-ov-file#basic-usage-1)

#### `.ConnectionStrings.DbKind`

Kind for your DB. Please use below.

- `"MariaDB"`
- `"SQLite"`

Note: If you want to use MySQL, specify `"MariaDB"`. The deference will be fixed by EFCore.

#### `.VRChat.LogFileDir`

Path for VRChat log directory.

If empty specified, automatically set your local machine's application data folder.

Only who changes log output folder can use this option.

### VRChatActivityLog.db

This is the database file that is created when you run VRChatActivityLogger.exe.

The contents are SQLite3 data, so it can be used in conjunction with other applications.

This file name can variable due to the connection string.

### VRChatActivityDbMigrator.exe

This application migrates records between DB engine and/or DB source.

See details for [VRChatActivityDbMigrator](VRChatActivityLogger/VRChatActivityDbMigrator/README.md)

##  Known issues

It is not possible to display the name of the user to whom invitations are sent from the invitations sending history, because the user name is not recorded in the VRChat log.

It is not possible to check connection to network DB, because `CanConnect()` on EFCore returns "DB exists or not".

## License  

This software is released under the MIT License, see LICENSE.