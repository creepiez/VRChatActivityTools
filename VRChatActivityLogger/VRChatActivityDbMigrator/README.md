VRChatActivityDbMigrator
====

VRChatActivityLoggerで使用するデータベースオブジェクトを別のデータベースに移行するアプリです。

SQLiteとMariaDB間でリレーションごと移行させる目的で製作しました。

This application is to migrate database objects to another database.

Designed for migrating whole relations between SQLite and MariaDB.

# 日本語
## 未実装箇所
- テーブルの個別作成機能
  - 部分的にテーブルが存在しない状態が考えられないので未実装です。
    EFCoreがテーブル単位の作成をサポートしていないのも理由の一つです。
    実装できますがめんどくさいので放置中。

## インストール方法
めんどくさいのでバイナリはありません。自分でビルドしてください。

## 使い方
先にappsettings.jsonを記入してください。
お好みのシェルから本アプリを起動してください。
移行完了後、VRChatActivityViewerかお好みのSQLクライアントで移行先DBをご覧ください。

### appsettings.json

記入例は下記です。`Source`と`Destination`の中身はVRChatActivityLoggerと同一です。

```json
{
  "Source": {
    "ConnectionStrings": {
      "VRChatActivityLog": "Data Source=C:\\Path\\To\\VRChatActivityLog.db;"
    },
    "DbKind": "SQLite"
  },

  "Destination": {
    "ConnectionStrings": {
      "VRChatActivityLog": "Server=localhost;Database=VRChatActivityLog;Username=usr;Password=pwd;"
    },
    "DbKind": "MariaDB"
  }
}
```

# English
## Not implemented
- Function to create by tables
  - Because of there is no such situations only one table missing.
    Another reason for the EFCore does not support create one table.
    Yes, there is solution for implementation but I'm lazy.

## Installing
No binary. Build yourself. I'm lazy.

## Usage
First make sure create `appsettings.json`.
Run this application with any shell which you like.
After migration, check destination db with VRChatActivityViewer or any SQL client which you like.

### appsettings.json

The sample is below. Members of `Source` and `Destination` are same as VRChatActivityLogger.

```json
{
  "Source": {
    "ConnectionStrings": {
      "VRChatActivityLog": "Data Source=C:\\Path\\To\\VRChatActivityLog.db;"
    },
    "DbKind": "SQLite"
  },

  "Destination": {
    "ConnectionStrings": {
      "VRChatActivityLog": "Server=localhost;Database=VRChatActivityLog;Username=usr;Password=pwd;"
    },
    "DbKind": "MariaDB"
  }
}
```



