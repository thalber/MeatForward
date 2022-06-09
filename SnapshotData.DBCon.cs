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

        internal readonly static string[] tables = new[] { DB_Roles, DB_Channels, DB_Overwrites, DB_Users };
        internal readonly static string[] withIds = new[] { DB_Roles, DB_Channels, DB_Users };
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
                //var cmd01 = DB.CreateCommand();
                //cmd01.CommandText = "PRAGMA main.";

                if (newDB)
                {
                    //var tablesToCheck = new[] { DB_Channels, DB_Roles, DB_Overwrites, DB_Users };
                    foreach (var tname in tables)
                    {
                        string tableheader = tname switch
                        {
                            DB_Channels => "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "NATIVEID TEXT NOT NULL, " +
                            "NAME TEXT NOT NULL, " +
                            "TYPE INTEGER, " +
                            "CATID TEXT, " +
                            "TOPIC TEXT",
                            DB_Roles => "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "NATIVEID INTEGER NOT NULL, " +
                            "NAME INTEGER NOT NULL, " +
                            "COLOR INTEGER, " +
                            "HOIST INTEGER NOT NULL, " +
                            "MENT INTEGER NOT NULL, " +
                            "PERMS INTEGER NOT NULL",
                            DB_Overwrites => "CHANNELID INTEGER NOT NULL, " +
                            "TARGETTYPE INTEGER NOT NULL, " +
                            "TARGETID INTEGER NOT NULL, " +
                            "PERMSALLOW INTEGER NOT NULL, " +
                            "PERMSDENY INTEGER NOT NULL",
                            DB_Users => "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "NATIVEID INTEGER NOT NULL",
                            _ => throw new ArgumentException()
                        };
                        var cmd = DB.CreateCommand();
                        var cmdtext = @$"CREATE TABLE {tname} ({tableheader})";
                        cmd.CommandText = cmdtext;
                        cmd.Parameters.AddWithValue("$tablename", tname);//Add(new SqliteParameter("$tablename", tname));
                        var r = cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine("Created record tables");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
        /// <summary>
        /// Records perm overwrites for a given channel
        /// </summary>
        /// <param name="channelid"></param>
        /// <param name="ows"></param>
        public void SetOverwrites(int channelid, Discord.Overwrite[] ows)
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
        public Discord.Overwrite[] GetOverwrites(int channelid)
        {
            //todo: test
            SqliteDataReader? r = default;
            List<Discord.Overwrite> res = new();
            SqliteCommand cmd1 = DB.CreateCommand();
            cmd1.CommandText = $"SELECT {DB_Overwrites}.*, {DB_Channels}.NATIVEID " +
                $"FROM {DB_Overwrites} " +
                $"INNER JOIN {DB_Channels} ON {DB_Overwrites}.CHANNELID={DB_Channels}.ID;";
            try
            {
                r = cmd1.ExecuteReader();
                if (!r.HasRows) goto done;
                var o_sc = r.GetSchemaTable();
                while (r.Read())
                {
                    Discord.Overwrite rpart = new(
                        (ulong)r.GetInt64(r.GetOrdinal("NATIVEID")),
                        (Discord.PermissionTarget)r.GetInt32(r.GetOrdinal("TARGETTYPE")),
                        new Discord.OverwritePermissions(
                            (ulong)r.GetInt64(r.GetOrdinal("PERMSALLOW")),
                            (ulong)r.GetInt64(r.GetOrdinal("PERMSDENY"))
                            ));
                    res.Add(rpart);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error retrieving overwrites for {channelid} : {e}");
            }
            finally
            {
                r?.Close();
                cmd1?.Dispose();
            }

        done:
            return res.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nativeid">discord channel native ID</param>
        /// <param name=""></param>
        /// <param name="ch"></param>
        /// <returns>resulting internal ID, null if failure</returns>
        public int? SetChannelData(ulong nativeid, channelVanityData ch)
        {
            SqliteDataReader? r = default;
            SqliteCommand cmd0 = DB.CreateCommand(), cmd1 = DB.CreateCommand();
            cmd0.CommandText = $"SELECT * FROM {DB_Channels} WHERE NATIVEID={nativeid}";
            int? res = default;
            bool alreadyKnown;
            Console.WriteLine($"Attempting to add channel record for {ch.name} ({nativeid}...)");



            try
            {
                refreshRes();

                cmd1.CommandText = alreadyKnown
                    //update existing record
                    ? $"UPDATE {DB_Channels} " +
                    $"SET NAME=$chname, " +
                    $"TYPE={(int?)ch.type ?? 0}, " +
                    $"CATID={ch.categoryId ?? (object)"NULL"}, " +
                    $"TOPIC=$chtopic " +
                    $"WHERE ID={res} "
                    //or create new record
                    : $"INSERT INTO {DB_Channels} " +
                    $"(NAME, NATIVEID, TYPE, CATID, TOPIC) " +
                    $"VALUES " +
                    $"($chname, {nativeid}, {(int?)ch.type ?? 0}, {ch.categoryId ?? (object)"NULL"}, $chtopic)";
                //cmd1.Parameters.AddWithValue("$chname", ch.name);
                //cmd1.Parameters.AddWithValue("$chtopic", ch.topic);
                cmd1.Parameters.AddWithValue("$chname", ch.name);
                cmd1.Parameters.AddWithValue("$chtopic", ch.topic ?? "NULL");
                //cmd1.Parameters.AddWithValue("$tp", ch.type);
                //cmd1.Parameters.AddWithValue("$catid", ch.categoryId);
                //cmd1.Parameters.AddWithValue("$ntid", nativeid);

                int c = cmd1.ExecuteNonQuery();

                refreshRes();
                Console.WriteLine($"Added channel record, recording overwrites");
                SetOverwrites(res.Value, ch.permOverwrites);

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
            }

            return res;

            void refreshRes()
            {
                try
                {
                    res = getEntityInternalID(nativeid, DB_Channels);
                    alreadyKnown = true;
                }
                catch (ArgumentException aex)
                {
                    Console.WriteLine(aex);
                    res = default;
                    alreadyKnown = false;
                }
            }
        }
        public channelVanityData? GetChannelData(int id)
        {
            //todo: test
            SqliteDataReader? rdata = default, odata = default;
            SqliteCommand cmd0 = DB.CreateCommand(), cmd1 = DB.CreateCommand();
            //var ;
            cmd0.CommandText = $"SELECT * FROM {DB_Channels} WHERE ID=&tid;";
            //cmd0.Parameters.AddWithValue("$tname", SQL_Channels);
            cmd0.Parameters.AddWithValue("$tid", id);

            //cmd1.CommandText = $"SELECT * FROM {SQL_Overwrites} WHERE ROLEID={id}";

            try
            {
                rdata = cmd0.ExecuteReader();
                if (!rdata.HasRows) return null;

                rdata.Read();

                channelVanityData res = new(
                    rdata.GetString(rdata.GetOrdinal("NAME")),
                    (Discord.ChannelType)rdata.GetInt32(rdata.GetOrdinal("TYPE")),
                    (ulong?)rdata.GetInt64(rdata.GetOrdinal("CATID")),
                    rdata.GetString(rdata.GetOrdinal("TOPIC")),
                    GetOverwrites(id));

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

        private int getEntityInternalID(ulong nativeID, string tablename)
        {
            if (!withIds.Contains(tablename)) throw new ArgumentException($"INVALID TABLE {tablename}!");
            int res = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            SqliteDataReader? r = default;
            cmd0.CommandText = @$"SELECT * FROM {tablename} WHERE NATIVEID={nativeID};";
            cmd0.Parameters.AddWithValue("$tname", tablename).SqliteType = SqliteType.Text;
            cmd0.Parameters.AddWithValue("$ntid", nativeID).SqliteType = SqliteType.Integer;
            r = cmd0.ExecuteReader();
            if (!r.HasRows) throw new ArgumentException($"{tablename} : record for {nativeID} not found!");
            r.Read();
            res = r.GetInt32(r.GetOrdinal("ID"));
            r?.Close();
            return res;
        }
        private void updateEntityInternalID(string tablename, int id, ulong newNative)
        {
            if (!withIds.Contains(tablename)) throw new ArgumentException("INVALID TABLE!");
            SqliteCommand cmd0 = DB.CreateCommand();
            cmd0.CommandText = $"UPDATE $tname " +
                $"SET NATIVEID=$ntid " +
                $"WHERE ID={id};";
            cmd0.Parameters.AddWithValue("$ntid", newNative);
            cmd0.ExecuteNonQuery();
        }

        public int? SetRoleData(ulong nativeID, roleVanityData rl)
        {
            bool alreadyKnown = false;
            SqliteDataReader r = null;
            SqliteCommand cmd0 = DB.CreateCommand();//, cmd1 = DB.CreateCommand();
            //cmd1.CommandText = $" ";
            int? res = default;

            try
            {
                refreshRes();
                cmd0.CommandText = alreadyKnown
                    //update existing record
                    ? $"UPDATE {DB_Roles} " +
                    $" SET COLOR={rl.col?.RawValue ?? 0}, " +
                    $"HOIST = {(rl.hoist ? 1 : 0)}, " +
                    $"MENT = {(rl.ment ? 1 : 0)}, " +
                    $"NAME=$rname, " +
                    $"PERMS={rl.perms} " +
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
                    @$"({nativeID}, {rl.col?.RawValue ?? 0}, {(rl.hoist ? 1 : 0)}, {(rl.ment ? 1 : 0)}, $rname, {rl.perms})";
                //@$"(&nid, $col, $hoist, $ment, $rname)";
                cmd0.Parameters.AddWithValue("$col", rl.col ?? (object)"NULL");
                cmd0.Parameters.AddWithValue("$hoist", rl.hoist);
                cmd0.Parameters.AddWithValue("$ment", rl.ment);
                cmd0.Parameters.AddWithValue("$rname", rl.name);
                cmd0.Parameters.AddWithValue("$nid", nativeID);
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
                    alreadyKnown = true;
                }
                catch (ArgumentException aex)
                {
                    res = default;
                    alreadyKnown = false;
                }
            }
        }
        public roleVanityData? GetRoleData(int id)
        {
            SqliteDataReader r = default;
            SqliteCommand cmd0 = DB.CreateCommand();
            roleVanityData? res = default;
            cmd0.CommandText = $"SELECT * FROM {DB_Roles} WHERE ID={id}";
            try
            {
                r = cmd0.ExecuteReader(System.Data.CommandBehavior.SingleRow);
                if (!r.HasRows) goto done;
                r.Read();
                res = new roleVanityData(new Discord.Color((uint)r.GetInt32(r.GetOrdinal("COLOR"))),
                    r.GetBoolean(r.GetOrdinal("HOIST")),
                    r.GetBoolean(r.GetOrdinal("MENT")),
                    (ulong)r.GetInt64(r.GetOrdinal("PERMS")),
                    r.GetString(r.GetOrdinal("NAME")));
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

        #region binds
        

        #endregion

    }
}