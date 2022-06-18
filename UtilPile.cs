using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

using static MeatForward.UtilPile;

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
        internal static SnapshotData.channelRecord getRecord(this Discord.WebSocket.SocketGuildChannel channel, IEnumerable<Overwrite>? ows = null)
        {
            SnapshotData.channelRecord data = new(channel.Id,
                channel.Name,
                channel.GetChannelType(),
                channel.getCatID(),
                (channel as ITextChannel)?.Topic,
                (channel as ITextChannel)?.IsNsfw ?? false,
                (channel as ITextChannel)?.SlowModeInterval,
                channel.Position,
                ows ?? ((channel is IThreadChannel tr) ? default(IEnumerable<Overwrite>) : channel.PermissionOverwrites));
            return data;
        }

        internal static SnapshotData.roleRecord getRecord(this Discord.WebSocket.SocketRole role)
        {
            SnapshotData.roleRecord res = new(role.Id, role.Color, role.IsHoisted, role.IsMentionable, role.Permissions.RawValue, role.Name);

            return res;
        }
        internal static SnapshotData.userRecord getRecord(this Discord.WebSocket.SocketGuildUser user)
        {
            return new SnapshotData.userRecord(user.Id,
                null,
                false,
                user.Username,
                user.Roles.Select(x => (default(int), x.Id)));
        }
        internal static SnapshotData.userRecord getUserRecord(this IBan ban)
        {
            return new SnapshotData.userRecord(ban.User.Id, ban.Reason, true, ban.User.Username, new List<(int, ulong)>());
        }

        internal static bool contentsEqual<T>(this IEnumerable<T> src, IEnumerable<T> other, Func<T, T, bool> comPred)
        {
            if (src is null || other is null) return false;
            if (src.Count() != other.Count()) return false;
            bool res = true;
            IEnumerator<T> srcen = src.GetEnumerator(),
                othen = other.GetEnumerator();
            while (srcen.MoveNext())
            {
                othen.MoveNext();

                res &= comPred(srcen.Current, othen.Current);
            }

            return res;
        }

        internal static string getColumnString(SqliteType tp, ColumnMods cfl)
        {
            StringBuilder sb = new();


            sb.Append(tp switch
            {
                SqliteType.Real => "REAL ",
                SqliteType.Integer => "INTEGER ",
                SqliteType.Text => "TEXT ",
                SqliteType.Blob or _ => "BLOB ",
            });

            if (cfl.HasFlag(ColumnMods.PrimeKey))
            {
                sb.Append("PRIMARY KEY ");
            }
            else
            {
                if (cfl.HasFlag(ColumnMods.Unique)) sb.Append("UNIQUE ");
                if (cfl.HasFlag(ColumnMods.NotNull)) sb.Append("NOT NULL ");
            }
            if (cfl.HasFlag(ColumnMods.Autoincrement)) sb.Append("AUTOINCREMENT ");
            return sb.ToString();
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

    [Flags]
    internal enum ColumnMods
    {
        None = 0b0,
        Unique = 0b01,
        NotNull = 0b10,
        PrimeKey = 0b11,
        Autoincrement = 0b100,
    }

    internal class tableTemplate : IEnumerable<columnHeaderInfo>
    {
        public List<columnHeaderInfo> columns = new();
        public void Add(string name, ColumnMods mode, SqliteType tp)
        {
            columns.Add(new columnHeaderInfo(name, mode, tp));
        }

        public IEnumerator<columnHeaderInfo> GetEnumerator()
        {
            return ((IEnumerable<columnHeaderInfo>)columns).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)columns).GetEnumerator();
        }
    }
    internal struct columnHeaderInfo// : IAmDataRow<columnHeaderInfo>
    {
        public string name;
        public ColumnMods modifiers;
        public SqliteType datatype;
        public columnHeaderInfo(string name, ColumnMods modifiers, SqliteType type)
        {
            this.datatype = type;
            this.name = name;
            this.modifiers = modifiers;
        }

        public string getColumnDefString()
            => $"{name} {getColumnString(datatype, modifiers)}";

        ///// <summary>
        ///// Intended to look at schema responses!
        ///// </summary>
        ///// <param name="r"></param>
        ///// <returns></returns>
        //public columnHeaderInfo fillFromCurrentRow(SqliteDataReader r)
        //{
        //    this.name = r.GetString(r.GetOrdinal("name"));
        //    this.datatype = r.GetString(r.GetOrdinal("type")) switch
        //    {
        //        "REAL" => SqliteType.Real,
        //        "INTEGER" => SqliteType.Integer,
        //        "BLOB" => SqliteType.Blob,
        //        "TEXT" or _ => SqliteType.Text
        //    };
        //    this.modifiers = default;
        //    if (r.GetBoolean(r.GetOrdinal("notnull"))) modifiers |= ColumnMods.NotNull;
        //    return this;
        //}

        //public Dictionary<string, (bool danger, object val)> postValues()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
