using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeatForward
{
    internal static class UtilPile
    {
        internal static bool IndexInRange<T>(this T[] arr, int index) => index > -1 && index < arr.Length;
        internal static ulong? getCatID (this Discord.WebSocket.SocketGuildChannel ch)
        {
            foreach (var cat in ch.Guild.CategoryChannels) if (cat.Channels.Contains(ch)) return cat.Id;
            return null;
        }
        internal static SnapshotData.channelStoreData getStoreData(this Discord.WebSocket.SocketGuildChannel channel, Overwrite[] ows = null)
        {
            SnapshotData.channelStoreData data = new(channel.Id,
                channel.Name,
                channel.GetChannelType(),
                channel.getCatID(),
                (channel as ITextChannel)?.Topic,
                (channel as ITextChannel)?.IsNsfw ?? false,
                (channel as ITextChannel)?.SlowModeInterval,
                channel.Position,
                channel.PermissionOverwrites.ToArray() ?? ows);
            return data;
        }

        internal static SnapshotData.roleStoreData getStoreData(this Discord.WebSocket.SocketRole role)
        {
            SnapshotData.roleStoreData res = new(role.Id, role.Color, role.IsHoisted, role.IsMentionable, role.Permissions.RawValue, role.Name);

            return res;
        }

        //internal static Dictionary<string, int> OrdinalsByNames(this Microsoft.Data.Sqlite.SqliteDataReader r)
        //{
        //    Dictionary<string, int> dict = new(); //Dictionary<string, int>();
        //    System.Data.DataTable schema = r.GetSchemaTable();
        //    foreach (var row in schema.Rows)
        //    {
        //        var actual = (System.Data.DataRow)row;
        //        dict.Add((string)actual["ColumnName"], (int)actual["Ordinal"]);
        //    }
        //    return dict;
        //}
    }

    internal class MeatActivity : IActivity
    {
        internal MeatActivity()
        {

        }

        public string aname;
        public string desc;
        public ActivityType atype = ActivityType.Watching;
        public string Name => aname;
        public ActivityType Type => atype;
        public ActivityProperties Flags => ActivityProperties.None;
        public string Details => desc;
    }
}
