//using Discord;
using Microsoft.Data.Sqlite;

namespace MeatForward
{
    internal partial class SnapshotData
    {
        public record struct userRecord : IAmDataRow<userRecord>, IHaveExtraSnapshotData<userRecord>//, IEquatable<userRecord>
        {
            public ulong nativeid;
            public string? banReason;
            public bool banned;
            public string? localName;

            //public IEnumerable<roleRecord> getRoles() { throw new NotImplementedException(); }

            public IEnumerable<(int, ulong)> attachedRoles;
            public IEnumerable<ulong> NIds => attachedRoles.Select(x => x.Item2);
            public IEnumerable<int> IIds => attachedRoles.Select(x => x.Item1);
            //public DateTime? eBanTimeBase;
            //public TimeSpan? eBanTimeRange;
            

            public int internalID = default;

            public userRecord(ulong nativeid, string? banReason, bool banned, string? localName, IEnumerable<(int, ulong)> roles) : this()
            {
                this.nativeid = nativeid;
                this.banReason = banReason;
                this.banned = banned;
                this.localName = localName;
                this.attachedRoles = roles;
            }

            public userRecord fillFromCurrentRow(SqliteDataReader r)
            {
                int o_nid = r.GetOrdinal("NATIVEID"),
                    o_BANED = r.GetOrdinal("BANNED"),
                    o_why = r.GetOrdinal("BANREASON"),
                    o_localname = r.GetOrdinal("LOCALNAME");
                nativeid = (ulong)r.GetInt64(o_nid);
                banned = r.GetBoolean(o_BANED);
                banReason = r.IsDBNull(o_why)? null : (string?)r.GetString(o_why);
                localName = r.IsDBNull(o_localname)? null : (string?)r.GetString(o_localname);
                return this;
            }


            public Dictionary<string, (bool danger, object? val)> postValues()
            {
                return new Dictionary<string, (bool danger, object? val)>()
                {
                    { "NATIVEID", (false, nativeid) },
                    { "BANNED", (false, banned ? 1 : 0) },
                    { "BANREASON", (true, (object?)banReason ?? DBNull.Value) },
                    { "LOCALNAME", (true, (object?)localName ?? DBNull.Value) }
                };
            }

            public userRecord fetchAdditionalData(SnapshotData sd)
            {
                this.attachedRoles = sd.GetRolesForUser(this.internalID);
                return this;
            }
        }
    }
}