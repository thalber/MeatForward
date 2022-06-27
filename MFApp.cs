//i finish this
//and then i round up

using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System.Diagnostics;

//using static System.Diagnostics.Debug;
using static MeatForward.ConsoleFace;
using static MeatForward.SnapshotData;

namespace MeatForward
{
    internal static partial class MFApp
    {
        private const string tokenKey = "MeatForward_TOKEN";
        private static DiscordSocketClient? _client;
        private static SnapshotData? _cSnap;
        private static SemaphoreSlim exitMark = new(0, 1);

        private static ChannelType[] CT_Order = new[]
        {
            ChannelType.Category,
            ChannelType.Text,
            ChannelType.Voice,
            ChannelType.Stage,
            ChannelType.PublicThread,
            ChannelType.PrivateThread,
        };
        //private static string _csnapJson 
        //    => _cSnap is not null 
        //    ? Newtonsoft.Json.JsonConvert.SerializeObject(_cSnap.props, Newtonsoft.Json.Formatting.Indented) 
        //    : "NULL";
        

        static async Task<int> Main(string[] args)
        {
            SQLitePCL.Batteries.Init();
            try
            {
                
                //for (int i = 0; i < args.Length; i++)
                //{
                //    var spl = args[i].Split('=', StringSplitOptions.RemoveEmptyEntries);
                //    var pname = spl.ElementAtOrDefault(0);
                //    var pct = spl.ElementAtOrDefault(1);
                //    switch (pname)
                //    {
                //        //todo: expand?
                //        case "-fp":
                //        case "--filepath":
                //            defaultFilepath = pct;
                //            break;
                //    }
                //}

                _client = new DiscordSocketClient(new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.All
                });
                _client.Log += (msg) => {
                    Console.WriteLine(msg);
                    return Task.CompletedTask;
                };
                //_client.MessageReceived += (arg) => {
                //    Console.WriteLine(arg.ToString());
                //    return Task.CompletedTask;
                //};
                _client.Ready += async () => {
                    foreach (var g in _client.Guilds)
                    {
                        Console.WriteLine($"{g.Name}, {g.Id}, {g.Channels.Count}");
                        foreach (var c in g.Channels) Console.WriteLine($"{c.Name}, {c.Id}, {c.GetChannelType()}");
                    }
                    //_is = new(_client);
                    _ = Task.Run(RunMainLoop);
                };
                await _client.LoginAsync(TokenType.Bot,
                    Environment.GetEnvironmentVariable(tokenKey, EnvironmentVariableTarget.User),
                    true);
                await _client.StartAsync();
                //Task.Delay(5000).Wait();
                exitMark.Wait();
                await _client.LogoutAsync();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled exception:\n" + e.ToString());
                return 1;
            }
        }

        //todo: user role caching test, reapply impl
        public static async void RunMainLoop()
        {
        mainLoop:
            //inloop repeate use vars
            SocketGuild? guild;
            SocketGuild[] allguilds = _client.Guilds.ToArray();
            SnapshotData.SnapshotProperties props = default;
            await _client.SetActivityAsync(new MeatActivity() { aname = "the pink mist pass by", desc = "", atype = ActivityType.Watching });
            
            Console.WriteLine();
            Console.WriteLine($"Current snapshot: {_cSnap ?? "NULL!" as object}");
            var r = cPrompt("Select needed action: ",
                new[] { "create", "capture", "rollback", "open", "close", "props", "send", "backup", "exit" },
                true);
            //main loop
            //todo: save and restore in order: cats -> channels -> threads
            switch (r)
            {
                case "create":
                    {
                        string path
                            = cPromptAny("Enter snapshot path ");
                        if (Directory.Exists(path))
                        {
                            Console.WriteLine("Path not empty!");
                            break;
                        }
                        try
                        {
                            guild = cPrompt("Select guild", allguilds, true);
                        }
                        catch
                        {
                            Console.WriteLine("No guilds available!");
                            break;
                        }
                        //if (cPromptBinary("Set password? ")) password = cPromptAny("Enter password (you will not be able to open the database without it later)");
                        var smode = cPromptFlags<SnapshotMode>("Select snapshot mode");
                        props = new(cPromptAny("Comment?"),
                            DateTime.UtcNow,
                            smode,
                            guild.Id);
                        _cSnap = new SnapshotData(path, null, props);
                        Console.WriteLine($"Created snapshot: {_cSnap}");
                        break;
                    }
                case "capture":
                    {
                        RequestOptions rqp = new()
                        {
                            AuditLogReason = "Snapshot update",
                            RetryMode = RetryMode.AlwaysRetry
                        };
                        if (_cSnap is null)
                        {
                            Console.WriteLine("no snapshot to capture to!");
                            break;
                        }
                        guild = _client.GetGuild(_cSnap.props.guildID);
                        if (guild is null)
                        {
                            Console.WriteLine("Not in target guild!");
                            break;
                        }
                        var usersDownload = guild.DownloadUsersAsync();
                        var bans = guild.GetBansAsync();
                        //TODO: add ban records
                        foreach (var role in guild.Roles)
                        {
                            _cSnap.SetRoleData(role.Id, role.getRecord());
                        }
#warning doesn't work; change to specific channel set props
                        foreach (var ct in CT_Order)
                        {
                            Console.WriteLine($" ? Archiving {ct} channels...");
                            foreach (var channel in guild.Channels.TakeWhile(xx => xx.GetChannelType().HasValue && xx.GetChannelType().Value == ct))
                            {
                                _cSnap.SetChannelData(channel.Id, channel.getRecord().fetchAdditionalData(_cSnap)
                                    /*needed because janky internal/native transitions*/);
                            }
                        }
                        await usersDownload;
                        Console.WriteLine("! Recording users!! count: " + guild.Users.Count);
                        foreach (SocketGuildUser? u in guild.Users)
                        {
                            try
                            {
                                var rec = u.getRecord();
                                //todo: revise the whole role binding process
                                Console.WriteLine(rec.attachedRoles.Count());
                                rec.attachedRoles = _cSnap.joinRolesWithInternalIDs(rec.NIds);
                                Console.WriteLine(rec.attachedRoles.Count());
                                _cSnap.setUserData(u.Id, rec);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error recording user {u.Username} : {ex}");
                            }
                            
                        }
                        await foreach (var banGroup in bans)
                        {
                            foreach (var ban in banGroup)
                            {
                                _cSnap.setUserData(ban.User.Id, ban.getUserRecord());
                            }
                        }

                        //trimming is no longer fucked :3
                        //trimming is once again fucked
                        _cSnap.trimChannels(guild.Channels.Select(xx => xx.Id), true);
                        _cSnap.trimRoles(guild.Roles.Select(xx => xx.Id), true);
                    }
                    //todo: users
                    _cSnap.props.creationDate = DateTime.UtcNow;
                    break;
                case "rollback":
                    {
                        int errc = default;
                        if (_cSnap is null) { Console.WriteLine("No snapshot open!"); break; }
                        guild = _client.GetGuild(_cSnap.props.guildID);
                        if (guild is null) { Console.WriteLine("Not in target guild!"); break; }
                        RequestOptions rqp = new() { AuditLogReason = $"Automated rollback from {_cSnap.ToString()}", RetryMode = RetryMode.AlwaysRetry };

                        bool recreateMissingRoles = cPromptBinary("Recreate missing roles?"),
                            recreateMissingChannels = cPromptBinary("Recreate missing channels?"),
                            nullify = _cSnap.props.smode.HasFlag(SnapshotMode.ForceNullifyOmitted);

                    restoreRoles:
                        if (!recreateMissingRoles) goto restoreChannels;
                        Console.WriteLine("! Recreating missing roles...");
                        List<(roleRecord record, Task<RestRole> t)> restoreRoleTasks = new();
                        foreach (var record in _cSnap.getAllRoleData())
                        {
                            if (!guild.Roles.Any(r => r.Id == record.nativeid))
                            {
                                Console.WriteLine($"Role {Newtonsoft.Json.JsonConvert.SerializeObject(record)} not found. Queueing up recreation");
                                restoreRoleTasks.Add(
                                    (record, 
                                    guild.CreateRoleAsync (record.name,
                                    new(record.perms),
                                    record.col,
                                    record.hoist,
                                    record.ment,
                                    rqp)));
                            }
                        }

                        foreach (var (record, t) in restoreRoleTasks)
                        {
                            try
                            {
                                var rl = await t;
                                _cSnap.updateEntityNativeID(DB_Roles,
                                record.internalId, rl.Id);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error restoring role! {ex}");
                                errc++;
                            }
                        }
                        Console.WriteLine($"Role restoration tasks ran: {restoreRoleTasks.Count}, errors: {errc}");
                        errc = 0;
                        restoreRoleTasks.Clear();

                    restoreChannels:
                        if (!recreateMissingChannels) goto restoreRolePerms;
                        //Console.WriteLine("! restoring");
                        List<(channelRecord record, Task t)> restoreChannelTasks = new();
                        var allChannels = _cSnap.getAllChannelData().ToArray();
                        Console.WriteLine("! Recreating missing channels...");

                        foreach (var ct in CT_Order)
                        {
                            Console.WriteLine($"? Restoring {ct} channels...");
                            foreach (var record in allChannels.TakeWhile(xx => xx.type == ct))
                            {
                                if (!guild.Channels.Any(ch => ch.Id == record.nativeid))
                                {
                                    Console.WriteLine($"Channel {record.name} : {record.internalID} (previously {record.nativeid}) not found. Queueing up recreation");
                                    restoreChannelTasks.Add((record,
                                        record.type switch
                                        {
                                        //todo: threads and category alignment
                                            ChannelType.Voice => guild.CreateVoiceChannelAsync(record.name, ch =>
                                            {
                                            //ch.PermissionOverwrites = record.permOverwrites;
                                                ch.CategoryId = record.parentNativeID;
                                                ch.Position = record.position is null or 0
                                                ? new()
                                                : new(record.position.Value);
                                            },
                                            rqp),
                                            (ChannelType.PrivateThread or ChannelType.PublicThread) when guild.GetChannel(record.parentNativeID ?? default) is SocketTextChannel tc 
                                            => tc.CreateThreadAsync(
                                                name: record.name,
                                                type: record.type is ChannelType.PublicThread ? ThreadType.PublicThread : ThreadType.PrivateThread,
                                                options: rqp),
                                            ChannelType.Category => guild.CreateCategoryChannelAsync(record.name, ch =>
                                            {
                                            //ch.PermissionOverwrites = record.permOverwrites;
                                                ch.Position = record.position is null or 0
                                                ? new()
                                                : new(record.position.Value);
                                            },
                                            rqp),
                                            ChannelType.Text or _ => guild.CreateTextChannelAsync(record.name, ch =>
                                            {
                                            //ch.PermissionOverwrites = record.permOverwrites;
                                                ch.Topic = record.topic;

                                                ch.CategoryId = record.parentNativeID;
                                                ch.IsNsfw = record.isNsfw;
                                                ch.SlowModeInterval = record.slowModeInterval is 0 or null
                                                ? new()
                                                : new(record.slowModeInterval.Value);
                                                ch.Position = record.position is null or 0
                                                ? new()
                                                : new(record.position.Value);
                                            },
                                            rqp)
                                        }));
                                }
                            }

                            foreach (var (record, t) in restoreChannelTasks)
                            {
                                try
                                {
                                    await t;
                                    var ct_text = t as Task<RestTextChannel>;
                                    var ct_voice = t as Task<RestVoiceChannel>;
                                    var ct_cat = t as Task<RestCategoryChannel>;
                                    var ct_thr = t as Task<SocketThreadChannel>;
                                    IChannel? result = ct_text?.Result ?? ct_voice?.Result ?? ct_cat?.Result ?? ct_thr?.Result as IChannel;
                                    if (result is null)
                                    {
                                        Console.WriteLine($"Unexpected null result in channel restore! " +
                                            $"Skipping {record.name}({record.internalID})");
                                        continue;
                                    }
                                    _cSnap.updateEntityNativeID(DB_Channels, record.internalID, result.Id);
                                    

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error recreating channel {record.name} ({record.internalID}): {ex}");
                                    errc++;
                                }
                            }
                            Console.WriteLine($"Ran {restoreChannelTasks.Count} channel restore tasks; errors : {errc}");
                            errc = 0;
                            restoreChannelTasks.Clear();
                        }

                    restoreRolePerms:;
                        List<(roleRecord record, Task t)> restoreRolePermTasks = new();
                        Console.WriteLine("! Restoring role perms...");
                        foreach (var roleRecord in _cSnap.getAllRoleData())
                        {
                            var role = guild.Roles.FirstOrDefault(rl => rl.Id == roleRecord.nativeid);
                            if (role is null) continue;
                            if (!role.getRecord().Equals(roleRecord))
                            {
                                Console.WriteLine($"{role.Name}'s permissions do not match the record, updating...");
                                restoreRolePermTasks.Add((roleRecord,
                                role.ModifyAsync(rl => {
                                    rl.Permissions = new GuildPermissions(roleRecord.perms);
                                    rl.Hoist = roleRecord.hoist;
                                    rl.Mentionable = roleRecord.ment;
                                    rl.Color = roleRecord.col ?? Color.Default;
                                    rl.Name = roleRecord.name;
                                },
                            rqp)));
                            }
                        }
                        foreach (var (record, t) in restoreRolePermTasks)
                        {
                            try
                            {
                                await t;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error restoring role perms for {record.name} ({record.nativeid}): {ex}");
                                errc++;
                            }
                        }
                        Console.WriteLine($"{restoreRolePermTasks.Count} role perm restore tasks ran, errors: {errc}");
                        errc = 0;
                        restoreRolePermTasks.Clear();

                    restoreChannelPerms:
                        List<(channelRecord record, Task t)> restoreChannelPermTasks = new();

                        Console.WriteLine("! restoring channel perm overwrites...");
                        Func<Overwrite, object> seld = xx => (xx.TargetId, xx.TargetType, xx.Permissions.AllowValue, xx.Permissions.DenyValue);
                        foreach (var record in _cSnap.getAllChannelData())
                        {
                            var channel = guild.Channels.FirstOrDefault(rl => rl.Id == record.nativeid);
                            if (channel is null) continue;
                            if (!channel.getRecord().Equals(record))
                            {
                                Console.WriteLine($"Overwrites for {channel.Name} ({channel.Id}) don't match! updating to {Newtonsoft.Json.JsonConvert.SerializeObject(record.permOverwrites.Select(seld))}");
                                restoreChannelPermTasks.Add((record, channel.ModifyAsync(ch =>
                                {
                                    ch.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(record.permOverwrites);
                                    
                                    //Console.WriteLine($"SCROM : {record.name}, {Newtonsoft.Json.JsonConvert.SerializeObject(ch.PermissionOverwrites.Value.Select(seld))}");
                                },
                                rqp)));
                            }
                        }
                        foreach (var (record, t) in restoreChannelPermTasks)
                        {
                            try
                            {
                                await t;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error restoring permissions for channel {record.name} ({record.nativeid} : {ex}");
                                errc++;
                            }
                        }
                        Console.WriteLine($"{restoreChannelPermTasks.Count} channel overwrite restore tasks ran, errors : {errc}");
                        errc = 0;
                        restoreChannelPermTasks.Clear();
                    }
                    break;
                case "open":
                    if (_cSnap is not null)
                    {
                        Console.WriteLine("There is a snapshot already opened! ");
                        break;
                    }
                    var name = cPromptAny("Enter snapshot address: >");
                    try
                    {
                        _cSnap = new SnapshotData(name, null);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("GENERAL LOAD ERROR: " + e);
                    }
                    break;
                case "close":
                    _cSnap?.Save();
                    _cSnap = null;
                    break;
                case "props":
                    if (_cSnap is null) break;
                    props = new(cPromptAny("Comment: >"), _cSnap.props.creationDate, cPromptFlags<SnapshotMode>("Select mode"), _cSnap.props.guildID);
                    _cSnap.props = props;
                    _cSnap.Save();
                    break;
                case "backup":
                    if (_cSnap is null) { Console.WriteLine("No snapshot!"); break; }

                    var dest = cPromptAny("Select destination: >");
                    if (Directory.Exists(dest)) { Console.WriteLine("Destination occupied!"); break; }

                    var bu = _cSnap.makeBackup(dest);
                    Console.WriteLine(bu is null ? "Couldn't create backup!" : $"Backup created: {bu}");
                    break;
                case "exit":
                    break;
                default:
                    Console.WriteLine("Command not implemented");
                    break;
            }
            if (r is not "exit") goto mainLoop;
            //end main loop
            //_cSnap = null;
            _cSnap?.Save();
            Console.WriteLine("Exiting...");
            exitMark.Release();
        }
    }

}