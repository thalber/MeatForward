//using Discord;
using Microsoft.Data.Sqlite;

namespace MeatForward
{
    internal partial class SnapshotData
    {
        public record struct userRecord : IAmDataRow<userRecord>//, IEquatable<userRecord>
        {
            public ulong nativeid;
            public string? banReason;
            public bool banned;
            public string? localName;

            //public IEnumerable<roleRecord> getRoles() { throw new NotImplementedException(); }

            private IEnumerable<int> attachedRoles;
            //public DateTime? eBanTimeBase;
            //public TimeSpan? eBanTimeRange;
            

            public int internalID = default;

            public userRecord(ulong nativeid, string? banReason, bool banned, string? localName, IEnumerable<int> attachedRoles) : this()
            {
                this.nativeid = nativeid;
                this.banReason = banReason;
                this.banned = banned;
                this.localName = localName;
                this.attachedRoles = attachedRoles;
            }

            public userRecord fillFromCurrentRow(SqliteDataReader r)
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, (bool danger, object val)> postValues()
            {
                throw new NotImplementedException();
            }
        }
    }
}