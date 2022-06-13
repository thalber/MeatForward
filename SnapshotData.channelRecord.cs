﻿//using Discord;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace MeatForward
{
    internal partial class SnapshotData
    {
        public record struct channelRecord : IAmDataRow<channelRecord>, IEquatable<channelRecord>
        {
            public string name;
            public Discord.ChannelType? type;
            public ulong? categoryId;
            public string? topic;
            public ulong nativeid;
            public bool isNsfw;
            public int? slowModeInterval;
            public int? position;

            /// <summary>
            /// ONLY right when fillFromCurrentRow() !!!
            /// </summary>
            public int internalID = default;

            //channelid, perm
            //public Dictionary<ulong, Overwrite[]> permOverwrites;// = new();
            public IEnumerable<Discord.Overwrite> permOverwrites;
            public channelRecord(ulong nativeid, string name, Discord.ChannelType? type, ulong? catID, string? topic, bool isNsfw, int? slowModeInterval, int? position, IEnumerable<Discord.Overwrite> overwrites)
            {
                this.nativeid = nativeid;
                this.name = name;
                this.type = type;
                this.categoryId = catID;
                this.topic = topic;
                this.isNsfw = isNsfw;
                this.slowModeInterval = slowModeInterval;
                this.position = position;
                this.permOverwrites = overwrites;
            }

            public bool Equals(channelRecord other)
            {
            ret:
                return this.name == other.name
                    && this.type == other.type
                    && this.categoryId == other.categoryId
                    && this.topic == other.topic
                    && this.isNsfw == other.isNsfw
                    && this.slowModeInterval == other.slowModeInterval
                    && this.permOverwrites.contentsEqual(other.permOverwrites,
                    (ow1, ow2)
                    => ow1.TargetId == ow2.TargetId
                    && ow1.TargetType == ow2.TargetType
                    && ow1.Permissions.AllowValue == ow2.Permissions.AllowValue
                    && ow1.Permissions.DenyValue == ow2.Permissions.DenyValue
                    );
            }

            //public override bool Equals([NotNullWhen(true)] object? obj)
            //{
            //    return (obj as channelRecord?)?.Equals(this) ?? base.Equals(obj);
            //}

            public channelRecord fetchOverwrites(SnapshotData sn)
            {
                this.permOverwrites = sn.GetOverwrites(this.internalID);
                //Console.WriteLine($"SCROM@: {this.name}, {permOverwrites.Count()}");
                return this;
            }
            public channelRecord fillFromCurrentRow(SqliteDataReader r)
            {
                this.type = (Discord.ChannelType?)r.GetInt32(r.GetOrdinal("TYPE"));
                this.name = r.GetString(r.GetOrdinal("NAME"));
                this.categoryId = r.IsDBNull(r.GetOrdinal("CATID")) ? null : (ulong?)r.GetInt64(r.GetOrdinal("CATID"));
                this.topic = r.GetString(r.GetOrdinal("TOPIC"));
                this.internalID = r.GetInt32(r.GetOrdinal("ID"));
                this.isNsfw = r.GetInt32(r.GetOrdinal("NSFW")) == 1;
                this.slowModeInterval = r.GetInt32(r.GetOrdinal("SLOWMODE"));
                this.position = r.GetInt32(r.GetOrdinal("POSITION"));
                this.nativeid = (ulong)r.GetInt64(r.GetOrdinal("NATIVEID"));
                Console.WriteLine(JsonConvert.SerializeObject(this));
                return this;
            }

            public Dictionary<string, (bool danger, object val)> postValues()
            {
                Dictionary<string, (bool danger, object val)> res = new()
                {
                    { "NAME", (true, name) },
                    { "TYPE", (false, (int)(type ?? Discord.ChannelType.Text)) },
                    { "CATID", (false, categoryId ?? (object)"NULL") },
                    { "TOPIC", (true, topic ?? "NULL") },
                    { "NSFW", (false, isNsfw) },
                    { "SLOWMODE", (false, slowModeInterval ?? 0) },
                    { "POSITION", (false, position ?? 0) },
                    { "NATIVEID", (false, nativeid) }
                };
                return res;
            }

            public override string? ToString()
            {
                return base.ToString();
            }
        }
    }
}