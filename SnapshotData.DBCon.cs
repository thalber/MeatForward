﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace MeatForward
{

    //#error figure out if nullables work right (please god i hope they work right)
#warning readd filtering
#warning inefficient use of IO, revise
    internal partial class SnapshotData
    {
        //id (int) : nativeid(int) : name (text) : color (int) : hoist (bool) : ment (bool)
        internal const string DB_Roles = "Roles";
        //nativeid (int)
        //todo: figure out how to deal with roles
        internal const string DB_Users = "Users";
        //id (int) : nativeid (int) : name (text) : topic (string) : type()
        internal const string DB_Channels = "Channels";
        internal const string DB_Overwrites = "Permissions";
        internal const string DB_RoleBindings = "RoleBindings";

        internal readonly static string[] tables = new[] { DB_Roles, DB_Channels, DB_Overwrites, DB_Users, DB_RoleBindings };
        internal readonly static string[] withIds = new[] { DB_Roles, DB_Channels, DB_Users };

        internal readonly static Dictionary<string, tableTemplate> tableTemplates = new()
        {
            { DB_Roles,
                new()
                {
                    { "ID", ColumnMods.PrimeKey | ColumnMods.Autoincrement, SqliteType.Integer },
                    { "NATIVEID", ColumnMods.NotNull, SqliteType.Integer },
                    { "NAME", ColumnMods.NotNull, SqliteType.Text },
                    { "COLOR", ColumnMods.NotNull, SqliteType.Integer },
                    { "HOIST", ColumnMods.NotNull, SqliteType.Integer },
                    { "MENT", ColumnMods.NotNull, SqliteType.Integer },
                    { "PERMS", ColumnMods.NotNull, SqliteType.Integer },
                } },
            { DB_Users, 
                new() 
                {
                    { "ID", ColumnMods.PrimeKey | ColumnMods.Autoincrement, SqliteType.Integer },
                    { "NATIVEID", ColumnMods.NotNull, SqliteType.Integer },
                    { "BANNED", ColumnMods.NotNull, SqliteType.Integer },
                    { "BANREASON", ColumnMods.None, SqliteType.Text },
                } },
            { DB_Overwrites,
                new()
                {
                    { "CHANNELID", ColumnMods.NotNull, SqliteType.Integer },
                    { "TARGETTYPE", ColumnMods.NotNull, SqliteType.Integer },
                    { "TARGETID", ColumnMods.NotNull, SqliteType.Integer },
                    { "PERMSALLOW", ColumnMods.NotNull, SqliteType.Integer },
                    { "PERMSDENY", ColumnMods.NotNull, SqliteType.Integer },
                } },
            { DB_Channels,
                new()
                {
                    { "ID", ColumnMods.PrimeKey | ColumnMods.Autoincrement, SqliteType.Integer},
                    { "NATIVEID", ColumnMods.NotNull, SqliteType.Integer },
                    { "NAME", ColumnMods.NotNull, SqliteType.Text },
                    { "TYPE", ColumnMods.None, SqliteType.Integer },
                    { "NSFW", ColumnMods.NotNull, SqliteType.Integer },
                    { "TOPIC", ColumnMods.None, SqliteType.Text },
                    { "CATID", ColumnMods.None, SqliteType.Integer },
                    { "SLOWMODE", ColumnMods.NotNull, SqliteType.Integer }
                } },
            {DB_RoleBindings,
                new()
                {
                    { "ROLEID", ColumnMods.NotNull, SqliteType.Integer },
                    { "USERID", ColumnMods.NotNull, SqliteType.Integer }
                } }
        };
        /// <summary>
        /// run *once*
        /// </summary>
        /// <param name="newDB"></param>
        /// <returns></returns>
        public bool linkDB(bool newDB)
        {
            DB.Open();
            try
            {
                Console.WriteLine($"Opening a DB; new : {newDB}");
                SqliteCommand cmdPokeTable = DB.CreateCommand(),
                    cmdAddMissingColumns = DB.CreateCommand();
                SqliteDataReader? r0 = default, r1 = default;
                //var cmd01 = DB.CreateCommand();
                //cmd01.CommandText = "PRAGMA main.";
                foreach (var table in tables)
                {
                    //i hate many things about this part
                    var template = tableTemplates[table];
                    try
                    {
                        System.Data.DataTable? schema = default;
                        cmdPokeTable.CommandText = $"SELECT * FROM {table};";
                        r0 = cmdPokeTable.ExecuteReader();//.GetSchemaTable();
                        r0.Close();
                        schema = r0.GetSchemaTable();
                        //
                        foreach (var templateColumn in template)
                        {
                            System.Data.DataRow? tarRow = null;
                            foreach (System.Data.DataRow row in schema.Rows)
                            {
                                if ((string)row["name"] == templateColumn.name) { tarRow = row; break; }
                            }
                            if (tarRow is null)
                            {
                                cmdAddMissingColumns.CommandText = $"ALTER TABLE {table} " +
                                    $"ADD ({templateColumn}";
                            }
                            //var ie = tableinfo.Rows as IEnumerable<object>;
                            //if (!tableinfo.Rows as IEnumerable<object>) { }// Get Any())
                        }
                        
                    }
                    catch (SqliteException ex)
                    {
                        Console.WriteLine($"Table {table} not found, creating");
                    }
                    finally { r0?.Close(); }
                }

                //if (newDB)
                //{
                //    //var tablesToCheck = new[] { DB_Channels, DB_Roles, DB_Overwrites, DB_Users };
                //    foreach (var tname in tables)
                //    {
                //        string tableheader = tname switch
                //        {
                //            DB_Channels => "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                //            "NATIVEID TEXT NOT NULL, " +
                //            "NAME TEXT NOT NULL, " +
                //            "TYPE INTEGER, " +
                //            "CATID TEXT, " +
                //            "TOPIC TEXT, " +
                //            "NSFW INTEGER NOT NULL, " +
                //            "SLOWMODE INTEGER NOT NULL, " +
                //            "POSITION INTEGER",
                //            DB_Roles => "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                //            "NATIVEID INTEGER NOT NULL, " +
                //            "NAME INTEGER NOT NULL, " +
                //            "COLOR INTEGER, " +
                //            "HOIST INTEGER NOT NULL, " +
                //            "MENT INTEGER NOT NULL, " +
                //            "PERMS INTEGER NOT NULL",
                //            DB_Overwrites => "CHANNELID INTEGER NOT NULL, " +
                //            "TARGETTYPE INTEGER NOT NULL, " +
                //            "TARGETID INTEGER NOT NULL, " +
                //            "PERMSALLOW INTEGER NOT NULL, " +
                //            "PERMSDENY INTEGER NOT NULL",
                //            DB_Users => "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                //            "NATIVEID INTEGER NOT NULL",
                //            _ => throw new ArgumentException()
                //        };
                //        var cmd = DB.CreateCommand();
                //        var cmdtext = @$"CREATE TABLE {tname} ({tableheader})";
                //        cmd.CommandText = cmdtext;
                //        cmd.Parameters.AddWithValue("$tablename", tname);//Add(new SqliteParameter("$tablename", tname));
                //        var r = cmd.ExecuteNonQuery();
                //    }
                //    Console.WriteLine("Created record tables");
                //}
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($" Error linking DB : {e}");
                return false;
            }
        }

        #region ows
        /// <summary>
        /// Records perm overwrites for a given channel
        /// </summary>
        /// <param name="channelid"></param>
        /// <param name="ows"></param>
        public void SetOverwrites(int channelid, IEnumerable<Discord.Overwrite> ows)
        {
            //todo: test
            SqliteDataReader? r = default;
            SqliteCommand cmd = DB.CreateCommand();
            cmd.CommandText = $"DELETE FROM {DB_Overwrites} WHERE CHANNELID={channelid};";
            try
            {
                //err, remove&readd?..
                Console.WriteLine($"{cmd.ExecuteNonQuery()} overwrite records removed...");
                foreach (var ow in ows) try
                    {

                        cmd.CommandText = $"INSERT INTO {DB_Overwrites} " +
                            $"(CHANNELID, TARGETTYPE, TARGETID, " +
                            $"PERMSALLOW, PERMSDENY) " +
                            $"VALUES " +
                            //#error check targets by internal IDs too
                            $"({channelid}, {(int)ow.TargetType}, {getEntityInternalID(ow.TargetId, (ow.TargetType is Discord.PermissionTarget.Role) ? DB_Roles : DB_Users)}, " +
                            $"{ow.Permissions.AllowValue}, {ow.Permissions.DenyValue})";
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Recorded permission overwrite to {channelid} for ({ow.TargetType}, {ow.TargetId})");
                        //cmd.Parameters[0].Value = ow.TargetId;

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error setting perm! {ex}");
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General rror setting OWs! {ex}");
            }
            finally
            {
                r?.Close();
                //cmd?.Dispose();
            }
        }
        public IEnumerable<Discord.Overwrite> GetOverwrites(int channelid)
        {
            return getRoleOverwrites(channelid).Concat(getUserOverwrites(channelid));//.ToArray();
        }

        internal IEnumerable<Discord.Overwrite> getRoleOverwrites(int channelid)
        {
            SqliteDataReader r = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            cmd0.CommandText = $"SELECT {DB_Overwrites}.*, {DB_Roles}.NATIVEID " +
                $"FROM {DB_Overwrites} INNER JOIN {DB_Roles} " +
                $"ON {DB_Overwrites}.TARGETID={DB_Roles}.ID AND {DB_Overwrites}.TARGETTYPE={(int)Discord.PermissionTarget.Role} " +
                $"WHERE {DB_Overwrites}.CHANNELID={channelid};";
            try
            {
                r = cmd0.ExecuteReader();
                if (!r.HasRows) goto imdone;
                while (r.Read())
                {
                    yield return new Discord.Overwrite((ulong)r.GetInt64(r.GetOrdinal("NATIVEID")),
                        Discord.PermissionTarget.Role,
                        new Discord.OverwritePermissions(
                            (ulong)r.GetInt64(r.GetOrdinal("PERMSALLOW")),
                            (ulong)r.GetInt64(r.GetOrdinal("PERMSDENY"))
                            )
                        );
                }
            }
            finally
            {
                r?.Close();
            }
        imdone:
            yield break;
        }
        internal IEnumerable<Discord.Overwrite> getUserOverwrites(int channelid)
        {
            SqliteDataReader r = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            cmd0.CommandText = $"SELECT {DB_Overwrites}.*, {DB_Users}.NATIVEID " +
                $"FROM {DB_Overwrites} INNER JOIN {DB_Users} " +
                $"ON {DB_Overwrites}.TARGETID={DB_Users}.ID AND {DB_Overwrites}.TARGETTYPE={(int)Discord.PermissionTarget.User} " +
                $"WHERE {DB_Overwrites}.CHANNELID={channelid};";
            try
            {
                r = cmd0.ExecuteReader();
                if (!r.HasRows) goto imdone;
                while (r.Read())
                {
                    yield return new Discord.Overwrite((ulong)r.GetInt64(r.GetOrdinal("NATIVEID")),
                        Discord.PermissionTarget.Role,
                        new Discord.OverwritePermissions(
                            (ulong)r.GetInt64(r.GetOrdinal("PERMSALLOW")),
                            (ulong)r.GetInt64(r.GetOrdinal("PERMSDENY"))
                            )
                        );
                }
            }
            finally
            {
                r?.Close();
            }
        imdone:
            yield break;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nativeid">discord channel native ID</param>
        /// <param name=""></param>
        /// <param name="ch"></param>
        /// <returns>resulting internal ID, null if failure</returns>

        #region idutils
        public int? getEntityInternalID(ulong nativeID, string tablename)
        {
            if (!withIds.Contains(tablename)) throw new ArgumentException($"INVALID TABLE {tablename}!");
            int? res = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            SqliteDataReader? r = default;
            cmd0.CommandText = @$"SELECT * FROM {tablename} WHERE NATIVEID={nativeID};";
            cmd0.Parameters.AddWithValue("$tname", tablename).SqliteType = SqliteType.Text;
            cmd0.Parameters.AddWithValue("$ntid", nativeID).SqliteType = SqliteType.Integer;
            r = cmd0.ExecuteReader();
            if (!r.HasRows) goto cret; //throw new ArgumentException($"{tablename} : record for {nativeID} not found!");
            r.Read();
            res = r.GetInt32(r.GetOrdinal("ID"));
        cret:;
            r?.Close();
            return res;
        }

        public void updateEntityNativeID(string tablename, int id, ulong newNative)
        {
            if (!withIds.Contains(tablename)) throw new ArgumentException("INVALID TABLE!");
            SqliteCommand cmd0 = DB.CreateCommand();
            cmd0.CommandText = $"UPDATE {tablename} " +
                $"SET NATIVEID={newNative} " +
                $"WHERE ID={id};";
            cmd0.Parameters.AddWithValue("$ntid", newNative);
            cmd0.Parameters.AddWithValue("&tname", tablename);
            cmd0.ExecuteNonQuery();
        }

        //todo: trim by arbitrary column
        private void trimTable(IEnumerable<int> idList, string tablename, bool whitelist = true)
        {
            if (!withIds.Contains(tablename)) throw new ArgumentException("Invalid table!");
            SqliteCommand cmd0 = DB.CreateCommand();
            if (!idList.Any()) { if (!whitelist) return; cmd0.CommandText = "DELETE FROM "; goto exec; }
            StringBuilder sb = new($"DELETE FROM {tablename} WHERE ID ");
            if (whitelist) sb.Append("NOT ");
            sb.Append("IN (");
            foreach (var id in idList)
            {
                sb.Append($"{id}, ");
            }
            sb.Length -= 2;
            sb.Append(")");
            cmd0.CommandText = sb.ToString();
        exec:;
            try
            {
                Console.WriteLine($"Trimming {tablename} : {cmd0.CommandText}");
                cmd0.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error trimming table {tablename}! {ex}");
            }
            finally
            {
                //r?.Close();
            }
        }

        #endregion
        #region channels
        public int? SetChannelData(ulong nativeid, channelRecord ch)
        {
            SqliteDataReader? r = default, r0 = default;
            SqliteCommand cmd0 = DB.CreateCommand(), cmd1 = DB.CreateCommand();
            cmd0.CommandText = $"SELECT * FROM {DB_Channels} WHERE NATIVEID={nativeid}";
            int? res = default;
            bool alreadyKnown;
            channelRecord chTest = default;
            Console.WriteLine($"Attempting to add channel record for {ch.name} ({nativeid}...)");

            try
            {
                refreshRes();
                if (res.HasValue)
                {
                    chTest = GetChannelData(res.Value) ?? default;
                }
                var vals = ch.postValues();
                cmd1.CommandText = alreadyKnown
                    //update existing record
                    ? $"UPDATE {DB_Channels} " +
                    $"SET NAME=$chname, " +
                    $"TYPE={vals["TYPE"].val}, " +
                    $"CATID={vals["CATID"].val}, " +
                    $"TOPIC=$chtopic, " +
                    $"NSFW={vals["NSFW"].val}, " +
                    $"SLOWMODE={vals["SLOWMODE"].val}," +
                    $"POSITION={vals["POSITION"].val} " +
                    $"WHERE ID={res} "
                    //or create new record
                    : $"INSERT INTO {DB_Channels} " +
                    $"(NAME, NATIVEID, TYPE, CATID, TOPIC, NSFW, SLOWMODE, POSITION) " +
                    $"VALUES " +
                    $"($chname, {nativeid}, {vals["TYPE"].val}, {vals["CATID"].val}, $chtopic, {vals["NSFW"].val}, {vals["SLOWMODE"].val}, {vals["POSITION"].val})";
                //cmd1.Parameters.AddWithValue("$chname", ch.name);
                //cmd1.Parameters.AddWithValue("$chtopic", ch.topic);
                cmd1.Parameters.AddWithValue("$chname", vals["NAME"].val);
                cmd1.Parameters.AddWithValue("$chtopic", vals["TOPIC"].val);
                //cmd1.Parameters.AddWithValue("$tp", ch.type);
                //cmd1.Parameters.AddWithValue("$catid", ch.categoryId);
                //cmd1.Parameters.AddWithValue("$ntid", nativeid);
                

                if (!chTest.Equals(ch))
                {
                    Console.WriteLine($"Added channel record, recording overwrites... {cmd1.CommandText}");
                    cmd1.ExecuteNonQuery();
                    refreshRes();
                    SetOverwrites(res.Value, ch.permOverwrites);
                }
                else Console.WriteLine("Channel data identical, skipping");

                

                //SetOverwrites()
                //foreach ()
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recording channel data: {ex}");
                res = null;
            }
            finally
            {
                if (!(r?.IsClosed ?? true)) r?.Close();
                r0?.Close();
            }

            return res;

            void refreshRes()
            {
                try
                {
                    res = getEntityInternalID(nativeid, DB_Channels);
                    alreadyKnown = res is not null;
                }
                catch (ArgumentException aex)
                {
                    Console.WriteLine(aex);
                    res = default;
                    alreadyKnown = false;
                }
            }
        }
        public channelRecord? GetChannelData(int id)
        {
            //todo: test
            SqliteDataReader? rdata = default, odata = default;
            SqliteCommand cmd0 = DB.CreateCommand(), cmd1 = DB.CreateCommand();
            //var ;
            cmd0.CommandText = $"SELECT * FROM {DB_Channels} WHERE ID={id};";
            //cmd0.Parameters.AddWithValue("$tname", SQL_Channels);
            //cmd0.Parameters.AddWithValue("$tid", id);

            //cmd1.CommandText = $"SELECT * FROM {SQL_Overwrites} WHERE ROLEID={id}";

            try
            {
                rdata = cmd0.ExecuteReader();
                if (!rdata.HasRows) return null;

                rdata.Read();

                channelRecord res = new channelRecord().fillFromCurrentRow(rdata).fetchOverwrites(this);
                    //rdata.GetString(rdata.GetOrdinal("NAME")),
                    //(Discord.ChannelType)rdata.GetInt32(rdata.GetOrdinal("TYPE")),
                    //(ulong?)rdata.GetInt64(rdata.GetOrdinal("CATID")),
                    //rdata.GetString(rdata.GetOrdinal("TOPIC")),
                    //GetOverwrites(id));

                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error retrieving channel data: {e}");
                return null;
            }
            finally
            {
                rdata?.Close();
                odata?.Close();
                cmd0?.Dispose();
                cmd1?.Dispose();
            }
        }

        //gross
        public void trimChannels(IEnumerable<ulong> nativeIDs, bool wl)
            => trimChannels(nativeIDs.Select(xx => getEntityInternalID(xx, DB_Channels)).SkipWhile(xx => xx is null).Cast<int>(), wl);
        public void trimChannels (IEnumerable<int> IDs, bool wl)
        {
            trimTable(IDs, DB_Channels, wl);
        }
        #endregion

        #region roles
        public int? SetRoleData(ulong nativeID, roleRecord rl)
        {
            bool alreadyKnown = false;
            SqliteDataReader r = null;
            SqliteCommand cmd0 = DB.CreateCommand();//, cmd1 = DB.CreateCommand();
            //cmd1.CommandText = $" ";
            int? res = default;

            try
            {
                refreshRes();

                var vals = rl.postValues();
                cmd0.CommandText = alreadyKnown
                    //update existing record
                    ? $"UPDATE {DB_Roles} " +
                    $"SET COLOR={vals["COLOR"].val}, " +
                    $"HOIST = {vals["HOIST"].val}, " +
                    $"MENT = {vals["MENT"].val}, " +
                    $"NAME=$rname, " +
                    $"PERMS={vals["PERMS"].val} " +
                    $"WHERE ID={res};"
                    //@$"SET COLOR=$col, " +
                    //@$"HOIST=$hoist, " +
                    //@$"MENT=$ment, " +
                    //@$"NAME=$rname " +
                    //@$"WHERE ID={res}"
                    //or create a new one
                    : @$"INSERT INTO " + DB_Roles + " " +
                    @$" (NATIVEID, COLOR, HOIST, MENT, NAME, PERMS) " +
                    @$"VALUES " +
                    @$"({nativeID}, {vals["COLOR"].val}, {vals["HOIST"].val}, {vals["MENT"].val}, $rname, {vals["PERMS"].val})";
                //@$"(&nid, $col, $hoist, $ment, $rname)";
                //cmd0.Parameters.AddWithValue("$col", rl.col ?? (object)"NULL");
                //cmd0.Parameters.AddWithValue("$hoist", rl.hoist);
                //cmd0.Parameters.AddWithValue("$ment", rl.ment);
                cmd0.Parameters.AddWithValue("$rname", vals["NAME"].val);
                //cmd0.Parameters.AddWithValue("$nid", nativeID);
                var c = cmd0.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting role data for {nativeID} ({rl.name}) : {ex}");
                Console.WriteLine(cmd0.CommandText);
                res = null;
            }
            finally
            {
                r?.Close();
            }

            return res;

            void refreshRes()
            {
                try
                {
                    res = getEntityInternalID(nativeID, DB_Roles);
                    alreadyKnown = res is not null;
                }
                catch (ArgumentException aex)
                {
                    res = default;
                    alreadyKnown = false;
                }
            }
        }
        public roleRecord? GetRoleData(int id)
        {
            SqliteDataReader r = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            roleRecord? res = default;
            cmd0.CommandText = $"SELECT * FROM {DB_Roles} WHERE ID={id}";
            try
            {
                r = cmd0.ExecuteReader(System.Data.CommandBehavior.SingleRow);
                if (!r.HasRows) goto done;
                r.Read();
                res = new roleRecord().fillFromCurrentRow(r);
                    //(ulong)r.GetInt64(r.GetOrdinal()) new Discord.Color((uint)r.GetInt32(r.GetOrdinal("COLOR"))),
                    //r.GetBoolean(r.GetOrdinal("HOIST")),
                    //r.GetBoolean(r.GetOrdinal("MENT")),
                    //(ulong)r.GetInt64(r.GetOrdinal("PERMS")),
                    //r.GetString(r.GetOrdinal("NAME")));
#warning impl
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving role data: {ex}");
            }
            finally
            {
                r?.Close();
            }
        done:
            return res;
        }

        public void trimRoles(IEnumerable<ulong> nativeIDs, bool wl) 
            => trimRoles(nativeIDs.Select(xx => getEntityInternalID(xx, DB_Roles)).SkipWhile(xx => xx is null).Cast<int>(), wl);

        public void trimRoles(IEnumerable<int> IDs, bool wl)
        {
            trimTable(IDs, DB_Roles, wl);
        }
        #endregion

        #region dispense
        public IEnumerable<roleRecord> getAllRoleData()
        {
            SqliteDataReader? r = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            cmd0.CommandText = $"SELECT * FROM {DB_Roles};";
            r = cmd0.ExecuteReader();

            try
            {
                if (!r.HasRows) goto done;
                while (r.Read())
                {
                    yield return new roleRecord().fillFromCurrentRow(r);
                }
            }
            finally
            {
                r?.Close();
            }
            done: yield break;
        }

        public IEnumerable<channelRecord> getAllChannelData()
        {
            SqliteDataReader? r = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            cmd0.CommandText = $"SELECT * FROM {DB_Channels};";
            try
            {
                r = cmd0.ExecuteReader();
                if (!r.HasRows) goto done;
                while (r.Read())
                {
                    yield return new channelRecord().fillFromCurrentRow(r).fetchOverwrites(this);
                }
            }
            finally { r?.Close(); }


            done:;
            yield break;
        }
        #endregion

    }
}
