using Discord;

namespace MeatForward
{
    internal static partial class MFApp
    {
        public class SnapshotData
        {
            public SnapshotData()
            {
                
            }
            
            public DateTime creationDate;
            public SnapshotMode smode;
            public ulong guildID;
            //
            public List<ulong> exceptRoles = new();
            public List<ulong> exceptUsers = new();
            public List<ulong> exceptChannels = new();
            //roleid, name
            public Dictionary<ulong, roleVanityData> roleData = new();
            public Dictionary<ulong, channelVanityData> channelData = new();
            //roleid, perm-rawval
            //public Dictionary<ulong, ulong> rolePerms = new();

            public struct channelVanityData
            {
                public string name;
                public ChannelType? type;
                public ulong? categoryId;
                public string topic;

                //channelid, perm
                //public Dictionary<ulong, Overwrite[]> permOverwrites;// = new();
                public Overwrite[] permOverwrites;
                public channelVanityData(string name, ChannelType? type, ulong? catID, string topic, Overwrite[] overwrites)
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
                public Color? col;
                public bool sep;
                public bool ment;
                public ulong perms;
                public string name;

                public roleVanityData(Color? col, bool sep, bool ment, ulong perms, string name)
                {
                    this.col = col;
                    this.sep = sep;
                    this.ment = ment;
                    this.perms = perms;
                    this.name = name;
                }
            }
        }
        
    }
}