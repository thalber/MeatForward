//using Discord;
using Microsoft.Data.Sqlite;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace MeatForward
{
    internal partial class SnapshotData
    {
        public SnapshotData(string path, string? password, SnapshotProperties? givenProps = default)
        {
            
            root = path;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            bool amNew = !File.Exists(dbPath);
            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Password = password
            };
            DB = new SqliteConnection(csb.ConnectionString);
            if (File.Exists(propPath))
                props = JsonConvert.DeserializeObject<SnapshotProperties>(File.ReadAllText(propPath));
            else props = givenProps ?? new();
            linkDB(amNew);
            Save();
        }

        public void Save()
        {
            File.WriteAllText(propPath, JsonConvert.SerializeObject(props));
            DB.Close();
            DB.Open();
        }
        public void Reload()
        {
#warning impl
        }
        ~SnapshotData()
        {
            DB?.Close();
            DB?.Dispose();
            //File.WriteAllText(propPath, JsonConvert.SerializeObject());
        }

        public readonly string root;
        public string propPath => Path.Combine(root, "props.json");
        public string dbPath => Path.Combine(root, "payload.db");
        public SnapshotProperties props;
        public readonly SqliteConnection DB;
        
        //roleid, name
        //public Dictionary<ulong, roleVanityData> roleData = new();
        //public Dictionary<ulong, channelVanityData> channelData = new();

        //public ulong guildID { set { props.guildID = value; } }
        //public string? comment { set { props.comment = value; } }

        public override string ToString()
            => $"Snapshot {props.guildID} // {props.creationDate}" + props.comment is null ? $" // {props.comment}" : string.Empty;

        public struct SnapshotProperties
        {
            public string? comment;
            public DateTime creationDate;
            public SnapshotMode smode;
            public ulong guildID;
            //
            public List<ulong> exceptRoles = new();
            public List<ulong> exceptUsers = new();
            public List<ulong> exceptChannels = new();

            public SnapshotProperties(string comment, DateTime creationDate, SnapshotMode smode, ulong guildID) : this()
            {
                this.comment = comment;
                this.creationDate = creationDate;
                this.smode = smode;
                this.guildID = guildID;
            }
        }
        public struct channelStoreData : IAmDataRow<channelStoreData>, IEquatable<channelStoreData>
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
            public Discord.Overwrite[] permOverwrites;
            public channelStoreData(ulong nativeid, string name, Discord.ChannelType? type, ulong? catID, string? topic, bool isNsfw, int? slowModeInterval, int? position, Discord.Overwrite[] overwrites)
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

            public bool Equals(channelStoreData other)
            {
                bool owMatch = true;
                if (this.permOverwrites is null) goto ret;
                foreach (var ow in this.permOverwrites)
                {
                    owMatch &= other.permOverwrites.Any(opo 
                        => opo.Permissions.AllowValue == ow.Permissions.AllowValue
                        && opo.Permissions.DenyValue == ow.Permissions.DenyValue
                        && opo.TargetType == ow.TargetType
                        && opo.TargetId == ow.TargetId);
                }
                ret:
                return this.name == other.name
                    && this.type == other.type
                    && this.categoryId == other.categoryId
                    && this.topic == other.topic
                    && this.isNsfw == other.isNsfw
                    && this.slowModeInterval == other.slowModeInterval
                    && owMatch;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return (obj as channelStoreData?)?.Equals(this) ?? base.Equals(obj);
            }

            public channelStoreData fetchOverwrites(SnapshotData sn)
            {
                this.permOverwrites = sn.GetOverwrites(this.internalID);
                Console.WriteLine($"SCROM@: {this.name}, {permOverwrites.Count()}");
                return this;
            }
            public channelStoreData fillFromCurrentRow(SqliteDataReader r)
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
        public struct roleStoreData : IAmDataRow<roleStoreData>, IEquatable<roleStoreData>
        {
            public Discord.Color? col;
            public bool hoist;
            public bool ment;
            public ulong perms;
            public string name;
            public ulong nativeid;
            /// <summary>
            /// ONLY right when fillFromCurrentRow!!!
            /// </summary>
            public int internalId = default;

            public roleStoreData(ulong nativeid, Discord.Color? col, bool sep, bool ment, ulong perms, string name)
            {
                this.col = col;
                this.hoist = sep;
                this.ment = ment;
                this.perms = perms;
                this.name = name;
                this.nativeid = nativeid;
            }

            public bool Equals(roleStoreData other)
            {
                return this.col?.RawValue == other.col?.RawValue
                    && this.hoist == other.hoist
                    && this.ment == other.ment
                    && this.perms == other.perms
                    && this.name == other.name
                    && this.nativeid == other.nativeid;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return (obj as roleStoreData?)?.Equals(this) ?? base.Equals(obj);
            }

            public roleStoreData fillFromCurrentRow(SqliteDataReader r)
            {
                col = new Discord.Color((uint)r.GetInt32(r.GetOrdinal("COLOR")));
                hoist = r.GetBoolean(r.GetOrdinal("HOIST"));//(long)r["HOIST"] == 1;
                ment = r.GetBoolean(r.GetOrdinal("MENT"));//(long)r["MENT"] == 1;
                perms = (ulong)r.GetInt64(r.GetOrdinal("PERMS"));
                name = r.GetString(r.GetOrdinal("NAME"));
                nativeid = (ulong)r.GetInt64(r.GetOrdinal("NATIVEID"));
                internalId = r.GetInt32(r.GetOrdinal("ID"));
                return this;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(col, hoist, ment, perms, name, nativeid);
            }

            public Dictionary<string, (bool danger, object val)> postValues()
            {
                Dictionary<string, (bool danger, object val)> res = new()
                {
                    { "COLOR", (false, col?.RawValue ?? 0) },
                    { "HOIST", (false, hoist ? 1 : 0) },
                    { "MENT", (false, ment ? 1 : 0) },
                    { "PERMS", (false, (long)perms) },
                    { "NAME", (true, name) },
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