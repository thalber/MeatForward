//using Discord;

namespace MeatForward
{
    internal partial class SnapshotData
    {
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
    }
}