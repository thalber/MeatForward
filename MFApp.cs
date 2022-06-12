using Discord.WebSocket;
using Discord;
using Discord.Rest;

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
        //private static string _csnapJson 
        //    => _cSnap is not null 
        //    ? Newtonsoft.Json.JsonConvert.SerializeObject(_cSnap.props, Newtonsoft.Json.Formatting.Indented) 
        //    : "NULL";
        private static string? defaultFilepath;

        static async Task<int> Main(string[] args)
        {
            SQLitePCL.Batteries.Init();
            try
            {
                
                for (int i = 0; i < args.Length; i++)
                {
                    var spl = args[i].Split('=', StringSplitOptions.RemoveEmptyEntries);
                    var pname = spl.ElementAtOrDefault(0);
                    var pct = spl.ElementAtOrDefault(1);
                    switch (pname)
                    {
                        //todo: expand?
                        case "-fp":
                        case "--filepath":
                            defaultFilepath = pct;
                            break;
                    }
                }

                _client = new DiscordSocketClient();
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

        //todo: user role caching
        //putting that into json would be suboptimal, should consider making a mini sql db instead maybe?
        //actually i  should have started with that maybe
        //fuckkkkk aaaaaaaaaaaa
        public static async void RunMainLoop()
        {
        mainLoop:
            //inloop repeate use vars
            SocketGuild? guild;
            SocketGuild[] allguilds = _client.Guilds.ToArray();
            SnapshotData.SnapshotProperties props = default;
            await _client.SetActivityAsync(new MeatActivity() { aname = "the pink mist pass by", desc = "" });

            Console.WriteLine();
            Console.WriteLine($"Current snapshot: {_cSnap ?? "NULL!" as object}");
            var r = cPrompt("Select needed action: ",
                new[] { "create", "capture", "rollback", "open", "close", "props", "send", "backup", "exit" },
                true);
            //main loop
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

                        foreach (var role in guild.Roles)
                        {
                            _cSnap.SetRoleData(role.Id, role.getStoreData());
                        }
                        foreach (var channel in guild.Channels)
                        {
                            _cSnap.SetChannelData(channel.Id, channel.getStoreData());
                        }
                        //trimming is no longer fucked :3
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
                        List<(roleStoreData record, Task<RestRole> t)> restoreRoleTasks = new();
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

                        foreach (var rt in restoreRoleTasks)
                        {
                            try
                            {
                                var rl = await rt.t;
                                _cSnap.updateEntityNativeID(DB_Roles,
                                rt.record.internalId, rl.Id);
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
                        List<(channelStoreData record, Task t)> restoreChannelTasks = new();
                        Console.WriteLine("! Recreating missing channels...");
                        foreach (var record in _cSnap.getAllChannelData())
                        {
                            if (!guild.Channels.Any(ch => ch.Id == record.nativeid))
                            {
                                Console.WriteLine($"Channel {Newtonsoft.Json.JsonConvert.SerializeObject(record)}) not found. Queueing up recreation");
                                restoreChannelTasks.Add((record,
                                    record.type switch
                                    {
                                        ChannelType.Voice => guild.CreateVoiceChannelAsync(record.name, ch =>
                                        {
                                            //ch.PermissionOverwrites = record.permOverwrites;
                                            ch.CategoryId = record.categoryId;
                                            ch.Position = record.position is null or 0
                                            ? new()
                                            : new(record.position.Value);
                                        },
                                        rqp),
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
                                            ch.CategoryId = record.categoryId;
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

                        foreach (var ct in restoreChannelTasks)
                        {
                            try
                            {
                                await ct.t;
                                var ct_text = ct.t as Task<RestTextChannel>;
                                var ct_voice = ct.t as Task<RestVoiceChannel>;
                                var ct_cat = ct.t as Task<RestCategoryChannel>;
                                IChannel? result = ct_text?.Result ?? ct_voice?.Result ?? ct_cat?.Result as IChannel;
                                if (result is null) {
                                    Console.WriteLine($"Unexpected null result in channel restore! " +
                                        $"Skipping {ct.record.name}({ct.record.internalID})");
                                    continue;
                                }
                                _cSnap.updateEntityNativeID(DB_Channels, ct.record.internalID, result.Id);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error recreating channel {ct.record.name} ({ct.record.internalID}): {ex}");
                                errc++;
                            }
                        }
                        Console.WriteLine($"Ran {restoreChannelTasks.Count} channel restore tasks; errors : {errc}");
                        errc = 0;
                        restoreChannelTasks.Clear();

                    restoreRolePerms:;
                        List<(roleStoreData record, Task t)> restoreRolePermTasks = new();
                        foreach (var roleRecord in _cSnap.getAllRoleData())
                        {
                            var role = guild.Roles.FirstOrDefault(rl => rl.Id == roleRecord.nativeid);
                            if (role is null) continue;
                            if (!role.getStoreData().Equals(roleRecord))
                            {
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
                        foreach (var rpt in restoreRolePermTasks)
                        {
                            try
                            {
                                await rpt.t;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error restoring role perms for {rpt.record.name} ({rpt.record.nativeid}): {ex}");
                            }
                        }
                        restoreRolePermTasks.Clear();

                    restoreChannelPerms:
                        List<(channelStoreData record, Task t)> restoreChannelPermTasks = new();
                        Func<Overwrite, object> seld = xx => (xx.TargetId, xx.TargetType, xx.Permissions.AllowValue, xx.Permissions.DenyValue);
                        foreach (var record in _cSnap.getAllChannelData())
                        {
                            var channel = guild.Channels.FirstOrDefault(rl => rl.Id == record.nativeid);
                            if (channel is null) continue;
                            if (!channel.getStoreData().Equals(record))
                            {
                                Console.WriteLine($"Overwrites dont match! updating {Newtonsoft.Json.JsonConvert.SerializeObject(record.permOverwrites.Select(seld))}");
                                restoreChannelPermTasks.Add((record, channel.ModifyAsync(ch =>
                                {
                                    ch.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(record.permOverwrites);
                                    
                                    //Console.WriteLine($"SCROM : {record.name}, {Newtonsoft.Json.JsonConvert.SerializeObject(ch.PermissionOverwrites.Value.Select(seld))}");
                                },
                                rqp)));
                            }
                        }
                        foreach (var cprt in restoreChannelPermTasks)
                        {
                            try
                            {
                                await cprt.t;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error restoring permissions for channel {cprt.record.name} ({cprt.record.nativeid} : {ex}");
                            }
                        }
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