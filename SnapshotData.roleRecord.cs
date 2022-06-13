//using Discord;
using Microsoft.Data.Sqlite;

namespace MeatForward
{
    internal partial class SnapshotData
    {
        public record struct roleRecord : IAmDataRow<roleRecord>, IEquatable<roleRecord>
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

            public roleRecord(ulong nativeid, Discord.Color? col, bool sep, bool ment, ulong perms, string name)
            {
                this.col = col;
                this.hoist = sep;
                this.ment = ment;
                this.perms = perms;
                this.name = name;
                this.nativeid = nativeid;
            }

            //public bool Equals(roleRecord other)
            //{
            //    return this.col?.RawValue == other.col?.RawValue
            //        && this.hoist == other.hoist
            //        && this.ment == other.ment
            //        && this.perms == other.perms
            //        && this.name == other.name
            //        && this.nativeid == other.nativeid;
            //}

            //public override bool Equals([NotNullWhen(true)] object? obj)
            //{
            //    return (obj as roleRecord?)?.Equals(this) ?? base.Equals(obj);
            //}

            public roleRecord fillFromCurrentRow(SqliteDataReader r)
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
        }
    }
}