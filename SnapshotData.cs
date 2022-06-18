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

        public SnapshotData? makeBackup(string target)
        {
            SnapshotProperties nprops = this.props;
            SnapshotData? res = default;
            res = new SnapshotData(target, null, nprops);
            DB.BackupDatabase(res.DB);
            res.Save();
            res.Close();
            return res;
        }
        public void Save()
        {
            File.WriteAllText(propPath, JsonConvert.SerializeObject(props));
            DB.Close();
            DB.Open();
        }
        public void Reconnect()
        {
#warning impl
        }
        public void Close()
        {
            DB?.Close();
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
            => $"Snapshot {props.guildID} // {props.creationDate} " + (props.comment is not null ? $" // {props.comment}" : string.Empty);


        public interface IHaveExtraSnapshotData<TSelf> where TSelf : IHaveExtraSnapshotData<TSelf>
        {
            /// <summary>
            /// populate instance with additional data from snapshot
            /// </summary>
            /// <param name="sd">snapshot to take data from</param>
            /// <returns>itself</returns>
            public TSelf fetchAdditionalData(SnapshotData sd);

        }
    }
}