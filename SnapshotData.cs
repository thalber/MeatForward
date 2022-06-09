//using Discord;
using Microsoft.Data.Sqlite;
using System.IO;
using Newtonsoft.Json;

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
        public struct channelVanityData
        {
            public string name;
            public Discord.ChannelType? type;
            public ulong? categoryId;
            public string? topic;

            //channelid, perm
            //public Dictionary<ulong, Overwrite[]> permOverwrites;// = new();
            public Discord.Overwrite[] permOverwrites;
            public channelVanityData(string name, Discord.ChannelType? type, ulong? catID, string? topic, Discord.Overwrite[] overwrites)
            {
                this.name = name;
                this.type = type;
                this.categoryId = catID;
                this.topic = topic;
                this.permOverwrites = overwrites;
            }
        }
        public struct roleVanityData
        {
            public Discord.Color? col;
            public bool hoist;
            public bool ment;
            public ulong perms;
            public string name;

            public roleVanityData(Discord.Color? col, bool sep, bool ment, ulong perms, string name)
            {
                this.col = col;
                this.hoist = sep;
                this.ment = ment;
                this.perms = perms;
                this.name = name;
            }
        }
        
    }
}