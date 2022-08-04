//using Discord;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace MeatForward
{
    internal partial class SnapshotData
    {
        public record struct channelRecord : IAmDataRow<channelRecord>, IEquatable<channelRecord>, IHaveExtraSnapshotData<channelRecord>
        {
            public string name;
            public Discord.ChannelType? type;
#warning unsynced for store/retrieve
            public ulong? parentNativeID;
            public int? parentInternalID = null;
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
            public channelRecord(ulong nativeid, string name, Discord.ChannelType? type, ulong? parentNativeID, string? topic, bool isNsfw, int? slowModeInterval, int? position, IEnumerable<Discord.Overwrite> overwrites)
            {
                this.nativeid = nativeid;
                this.name = name;
                this.type = type;
                this.parentNativeID = parentNativeID;
                this.topic = topic;
                this.isNsfw = isNsfw;
                this.slowModeInterval = slowModeInterval;
                this.position = position;
                this.permOverwrites = overwrites;
                parentInternalID = default;
            }

            public bool Equals(channelRecord other)
            {
            ret:
                return this.name == other.name
                    && this.type == other.type
                    && (this.parentNativeID == other.parentNativeID || this.parentInternalID == other.parentInternalID)
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

            public channelRecord fetchAdditionalData(SnapshotData sd)
            {
                this.permOverwrites ??= sd.GetOverwrites(this.internalID);
                //Console.WriteLine($"SCROM@: {this.name}, {permOverwrites.Count()}");
                if (parentInternalID is not null && parentNativeID is null)
                {
                    parentNativeID = sd.getEntityNativeID(parentInternalID.Value, DB_Channels);
                }
                if (parentNativeID is not null && parentInternalID is null)
                {
                    parentInternalID = sd.getEntityInternalID(parentNativeID.Value, DB_Channels);
                }
                return this;
            }

            
            public channelRecord fillFromCurrentRow(SqliteDataReader r)
            {
                this.type = (Discord.ChannelType?)r.GetInt32(r.GetOrdinal("TYPE"));
                this.name = r.GetString(r.GetOrdinal("NAME"));
                this.parentInternalID = r.IsDBNull(r.GetOrdinal("PARENT")) ? null : r.GetInt32(r.GetOrdinal("PARENT"));
                this.topic = r.IsDBNull(r.GetOrdinal("TOPIC")) ? null : r.GetString(r.GetOrdinal("TOPIC"));
                this.internalID = r.GetInt32(r.GetOrdinal("ID"));
                this.isNsfw = r.GetInt32(r.GetOrdinal("NSFW")) == 1;
                this.slowModeInterval = r.GetInt32(r.GetOrdinal("SLOWMODE"));
                this.position = r.GetInt32(r.GetOrdinal("POSITION"));
                this.nativeid = (ulong)r.GetInt64(r.GetOrdinal("NATIVEID"));
                System.Diagnostics.Debug.WriteLine($"deser {this.name} :: {this.type}");
                //Console.WriteLine(JsonConvert.SerializeObject(this));
                return this;
            }

            public Dictionary<string, (bool danger, object val)> postValues()
            {
                Dictionary<string, (bool danger, object val)> res = new()
                {
                    { "NAME", (true, name) },
                    { "TYPE", (false, (int)(type ?? Discord.ChannelType.Text)) },
                    { "PARENT", (false, (object?)parentInternalID ?? "NULL") },
                    { "TOPIC", (true, topic ?? (object)DBNull.Value) },
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