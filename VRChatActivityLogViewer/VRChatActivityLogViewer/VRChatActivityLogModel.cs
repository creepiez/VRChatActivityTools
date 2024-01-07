﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VRChatActivityToolsShared.Database;

namespace VRChatActivityLogViewer
{
    /// <summary>
    /// VRChatでのアクティビティのモデル
    /// </summary>
    class VRChatActivityLogModel
    {
        /// <summary>
        /// アクティビティを検索する
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static async Task<List<ActivityLog>> SearchActivityLogsAsync(ActivityLogSearchParameter parameter, DbOperatorFactory factory)
        {
            var searchActivityTypes = new List<ActivityType>();
            if (parameter.IsJoinedRoom)
                searchActivityTypes.Add(ActivityType.JoinedRoom);
            if (parameter.IsMetPlayer)
                searchActivityTypes.Add(ActivityType.MetPlayer);
            if (parameter.IsSendInvite)
                searchActivityTypes.Add(ActivityType.SendInvite);
            if (parameter.IsSendRequestInvite)
                searchActivityTypes.Add(ActivityType.SendRequestInvite);
            if (parameter.IsReceivedInvite)
                searchActivityTypes.Add(ActivityType.ReceivedInvite);
            if (parameter.IsReceivedRequestInvite)
                searchActivityTypes.Add(ActivityType.ReceivedRequestInvite);
            if (parameter.IsSendFriendRequest)
                searchActivityTypes.Add(ActivityType.SendFriendRequest);
            if (parameter.IsReceivedFriendRequest)
                searchActivityTypes.Add(ActivityType.ReceivedFriendRequest);
            if (parameter.IsAcceptFriendRequest)
                searchActivityTypes.Add(ActivityType.AcceptFriendRequest);
            if (parameter.IsReceivedInviteResponse)
                searchActivityTypes.Add(ActivityType.ReceivedInviteResponse);
            if (parameter.IsReceivedRequestInviteResponse)
                searchActivityTypes.Add(ActivityType.ReceivedRequestInviteResponse);
            if (parameter.IsPlayedVideo)
                searchActivityTypes.Add(ActivityType.PlayedVideo);
            if (parameter.IsAcceptInvite)
                searchActivityTypes.Add(ActivityType.AcceptInvite);
            if (parameter.IsAcceptRequestInvite)
                searchActivityTypes.Add(ActivityType.AcceptRequestInvite);

            List<ActivityLog> activityLogs = new List<ActivityLog>();
            using (var db = factory.GetDbContext())
            {
                activityLogs = await db.ActivityLogs
                    .Where(a => searchActivityTypes.Contains(a.ActivityType))
                    .Where(a => a.Timestamp >= parameter.FromDateTime)
                    .Where(a => a.Timestamp < parameter.UntilDateTime)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
            }
            return activityLogs;
        }
    }
}
