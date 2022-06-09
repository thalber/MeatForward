using Discord.WebSocket;
using Discord;
using Discord.Rest;

using static MeatForward.ConsoleFace;

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
                new[] { "create", "capture", "rollback", "open", "close", "props", "send", "exit" },
                true);
            //main loop
            switch (r)
            {
                //case "0snapshot":
                //    {
                //    //    SnapshotMode mode;
                //    //    try
                //    //    {
                //    //        guild = cPrompt("Choose guild", allguilds.ToArray(), true);
                //    //    }
                //    //    catch (ArgumentException)
                //    //    {
                //    //        Console.WriteLine("No guilds available!");
                //    //        break;
                //    //    }
                //    //    await _client.SetActivityAsync(new MeatActivity()
                //    //    {
                //    //        atype = ActivityType.Listening,
                //    //        aname = "to the pulse closely",
                //    //        desc = guild.Name
                //    //    });
                //    //    mode = cPromptFlags<SnapshotMode>("Set snapshot mode");
                //    //    Console.WriteLine(mode);
                //    //    string snapName = cPromptAny("Enter snapshot name");
                //    //    string? pw = default;
                //    //    if (cPromptBinary("use a password?")) pw = cPromptAny("Enter password");
                //    //    SnapshotData snap = new(snapName, pw);
                //    //    excepts:
                //    //    var blprompts = new[]
                //    //    {
                //    //        ("users", snap.props.exceptUsers),
                //    //        ("roles", snap.props.exceptRoles),
                //    //        ("channels", snap.props.exceptRoles)
                //    //    };
                //    //    foreach (var bl in blprompts)
                //    //    {
                //    //        if (cPromptBinary($"Except some {bl.Item1}?"))
                //    //        {
                //    //            //get names
                //    //            var inputs = cPromptAny($"Input {bl.Item1} IDs or names: ").
                //    //                Split(' ', StringSplitOptions.RemoveEmptyEntries);
                //    //            foreach (var input in inputs)
                //    //            {
                //    //                if (ulong.TryParse(input, out ulong res)) bl.Item2.Add(res);
                //    //                switch (bl.Item1)
                //    //                {
                //    //                    case "roles": 
                //    //                        foreach (var role in guild.Roles)
                //    //                        {
                //    //                            if (role.Name == input) bl.Item2.Add(role.Id);
                //    //                            //snap.roleData.Add(role.Id, new(default, false, false, ) { };
                //    //                        }

                //    //                        break;
                //    //                    case "users": 
                //    //                        foreach(var user in guild.Users) 
                //    //                            if (user.Username == input) bl.Item2.Add(user.Id);
                //    //                        break;
                //    //                    case "channels":
                //    //                        foreach (var channel in guild.Channels)
                //    //                        {
                //    //                            if (channel.Name == input) bl.Item2.Add(channel.Id);
                //    //                            #warning what the fuck was i doing?
                //    //                            //snap.channelData.Add(channel.Id, new(channel.Name, 
                //    //                            //    channel.GetChannelType() ?? ChannelType.Text, 
                //    //                            //    channel.getCatID(), 
                //    //                            //    (channel as ITextChannel)?.Topic));
                //    //                        }
                //    //                        break;
                //    //                        #warning something else?
                //    //                }
                                    
                //    //            }
                //    //        }
                //    //    }
                //    //    //roleData:
                //    //    //foreach (var channel in guild.Channels)
                //    //    //{
                //    //    //    snap.channelData.Add(channel.Id, new(channel.Name, channel.GetChannelType(), channel.getCatID(), (channel as ITextChannel)?.Topic));
                //    //    //}
                //    //    //foreach (var role in guild.Roles)
                //    //    //{

                //    //    //}
                        
                //    //baseRolePerms:
                //    //    //List<(ulong roleid, ulong perms)> 
                //    //    foreach (var role in guild.Roles) snap.roleData.Add(role.Id, 
                //    //        new(role.Color, 
                //    //        role.IsHoisted, 
                //    //        role.IsMentionable, 
                //    //        role.Permissions.RawValue, 
                //    //        role.Name));

                //    //channelOverrides:
                //    //    if (!mode.HasFlag(SnapshotMode.SaveOverrides)) goto endMakeSnap;
                //    //    foreach (var channel in guild.Channels)
                //    //    {
                //    //        snap.channelData.Add(
                //    //            channel.Id,
                //    //            new SnapshotData.channelVanityData(channel.Name,
                //    //                channel.GetChannelType() ?? ChannelType.Text,
                //    //                channel.getCatID(),
                //    //                (channel as ITextChannel)?.Topic,
                //    //                channel.PermissionOverwrites.ToArray()));
                //    //        //.PermissionOverwrites.First().
                //    //    }
                //    //    if (cPromptBinary("Add comment?")) snap.comment = cPromptAny("Enter comment: ");

                //    //    snap.creationDate = DateTime.UtcNow;
                //    //    _cSnap = snap;
                //    //endMakeSnap: break;
                //    }
                //case "0rollback":
                //    {
                //    //    //todo: cover all excepts everywhere
                //    //    //todo: better error logging
                //    //    if (_cSnap is null) { Console.WriteLine("No snapshot for rollback!"); goto endRollback; }
                //    //    Console.WriteLine($"Current snapshot is for guild: {_cSnap.guildID}.");
                //    //    guild = allguilds.FirstOrDefault(g => g.Id == _cSnap.guildID);
                //    //    if (guild is null) { Console.WriteLine("Not in target guild! aborting"); break; }

                //    //    await _client.SetActivityAsync(new MeatActivity()
                //    //    {
                //    //        atype = ActivityType.Playing,
                //    //        aname = "with veins",
                //    //        desc = ""
                //    //    });
                //    //    //SnapshotMode md () => _cSnap.smode;
                //    //    //var md = _cSnap.smode;
                //    //    var mReason = $"Automated rollback to snapshot {_cSnap.creationDate}";
                //    //    RequestOptions rqParams = new() { AuditLogReason = mReason, RetryMode = RetryMode.AlwaysRetry };
                //    //    if (cPromptBinary($"Current mode: {_cSnap.smode}. Update?"))
                //    //        _cSnap.smode = cPromptFlags<SnapshotMode>(null);

                //    //    bool whitelist = _cSnap.smode.HasFlag(SnapshotMode.Whitelist),
                //    //        nullify = _cSnap.smode.HasFlag(SnapshotMode.ForceNullifyOmitted),
                //    //        overrides = _cSnap.smode.HasFlag(SnapshotMode.SaveOverrides),
                //    //        restoreRoleNames = cPromptBinary("Restore role names?"),
                //    //        recreateLostRoles = cPromptBinary("Recreate removed roles?"),
                //    //        restoreChannelNames = cPromptBinary("Restore channel names?"),
                //    //        recreateLostChannels = cPromptBinary("Recreate removed channels?");

                //    //    //recover deleted if needed
                //    //    List<(Task<RestRole> task, ulong oldID)> roleRecreateTasks = new();

                //    //    //recreate roles
                //    //    foreach (var sr in _cSnap.roleData)
                //    //    {
                //    //        var role = guild.Roles.FirstOrDefault(tr => tr.Id == sr.Key);
                //    //        if (role is null && recreateLostRoles) roleRecreateTasks.Add((
                //    //            guild.CreateRoleAsync(sr.Value.name, 
                //    //            color:sr.Value.col, 
                //    //            isHoisted:sr.Value.sep, 
                //    //            isMentionable:sr.Value.ment, 
                //    //            permissions:new(sr.Value.perms),
                //    //            options: rqParams 
                //    //            ), sr.Key));
                //    //    }


                //    //    foreach (var t in roleRecreateTasks)
                //    //    {
                //    //        try
                //    //        {
                //    //            //t.task.Wait();
                //    //            var res = await t.task;//t.task.Result;
                //    //            if (res is null) continue;
                //    //            //update ID on roledata
                //    //            var oldData = _cSnap.roleData[t.oldID];
                //    //            _cSnap.roleData.Remove(t.oldID);
                //    //            _cSnap.roleData.Add(res.Id, oldData);
                //    //        }
                //    //        catch (Exception e)
                //    //        {
                //    //            Console.WriteLine($"Failed to restore role: {e}");
                //    //        }
                //    //    }
                //    //    roleRecreateTasks.Clear();
                //    //    //roleRestoreTasks.Clear();

                //    //    //RestCategoryChannel plain = default;
                //    //    //IChannel plainCastTest = plain;

                //    //    //Task<Discord.Rest.RestTextChannel> generic = default;
                //    //    //Task<IChannel> genericCastTest = generic;

                //    //    //the latter is impossible because generics can't be cast properly
                //    //    //fuck you c#

                //    //    List<(Task task, ulong oldId)> channelRecreateTasks = new();
                //    //    //restore channels
                //    //    foreach (var sc in _cSnap.channelData)
                //    //    {
                //    //        var channel = guild.Channels.FirstOrDefault(tc => tc.Id == sc.Key);
                //    //        if (channel is null && recreateLostChannels) switch (sc.Value.type)
                //    //            {
                //    //                case ChannelType.Text:
                //    //                    channelRecreateTasks.Add(
                //    //                        (guild.CreateTextChannelAsync(sc.Value.name,
                //    //                        ntc => { ntc.Name = sc.Value.name; 
                //    //                            ntc.CategoryId = sc.Value.categoryId; 
                //    //                            ntc.Topic = sc.Value.topic ?? String.Empty; },
                //    //                        rqParams), 
                //    //                        sc.Key)
                //    //                        ); 
                //    //                    break;
                //    //                case ChannelType.Voice:
                //    //                    channelRecreateTasks.Add(
                //    //                        (guild.CreateVoiceChannelAsync(sc.Value.name,
                //    //                        nvc => { nvc.Name = sc.Value.name; nvc.CategoryId = sc.Value.categoryId; },
                //    //                        rqParams), sc.Key)
                //    //                        );
                //    //                    break;
                //    //                case ChannelType.Category:
                //    //                    channelRecreateTasks.Add(
                //    //                        (guild.CreateCategoryChannelAsync(sc.Value.name,
                //    //                        ncc => { ncc.Name = sc.Value.name; }, 
                //    //                        rqParams), sc.Key)
                //    //                        );
                //    //                    break;
                //    //                default:
                //    //                    Console.WriteLine();
                //    //                    break;

                //    //            }
                //    //    }
                //    //    foreach (var t in channelRecreateTasks)
                //    //    {
                //    //        try
                //    //        {
                //    //            await t.task;
                //    //            //var res = t.Res;
                //    //            //if (t is Task<Discord.Rest.>)
                //    //            //dynamic res;
                //    //            var t_tc = t.task as Task<RestTextChannel>;
                //    //            var t_vc = t.task as Task<RestVoiceChannel>;
                //    //            var t_cc = t.task as Task<RestCategoryChannel>;
                //    //            IChannel res = t_tc?.Result ?? t_vc?.Result ?? t_cc?.Result as IChannel;
                //    //            if (res is null) continue;
                //    //            var oldData = _cSnap.channelData[t.oldId];
                //    //            _cSnap.channelData.Remove(t.oldId);
                //    //            _cSnap.channelData.Add(res.Id, oldData);
                //    //            //res ??= t_cc?.Result;
                //    //            //var oldData = _cSnap.channelData.
                //    //        }
                //    //        catch (Exception e)
                //    //        {
                //    //            Console.WriteLine($"Failed to recreate channel: {e}");
                //    //        }
                //    //    }
                //    //    List<(Task task, IRole r)> rolePermRestoreTasks = new();

                //    //    setRolePerms:
                //    //    bool roleSelected(ulong r) 
                //    //        => _cSnap.exceptRoles.Contains(r) ^ !whitelist;
                //    //    bool userSelected(ulong u) 
                //    //        => _cSnap.exceptUsers.Contains(u) ^ !whitelist;
                //    //    bool channelSelected(ulong c)
                //    //        => _cSnap.exceptChannels.Contains(c) ^ !whitelist;

                //    //    foreach (var role in guild.Roles)
                //    //    {
                //    //        if (roleSelected(role.Id)) rolePermRestoreTasks.Add((role.ModifyAsync(rp => { 
                //    //            rp.Permissions = new(new(_cSnap.roleData[role.Id].perms)); },
                //    //            options:rqParams), role));
                //    //        else if (nullify)
                //    //        {
                //    //            rolePermRestoreTasks.Add((role.ModifyAsync(rp => 
                //    //            rp.Permissions = new(new(0)),
                //    //            options:rqParams), role));
                //    //        }
                //    //    }

                //    //    foreach (var t in rolePermRestoreTasks)
                //    //    {
                //    //        try
                //    //        {
                //    //            await t.Item1;
                //    //        }
                //    //        catch (Exception e) { Console.WriteLine($"Could not restore role perms for {t.r.Name} {t.r.Id}: {e}"); }
                //    //    }

                //    //    _client.PurgeDMChannelCache();

                //    //    setChannelOverrides:
                //    //    List<Task> channelPermRestoreTasks = new();
                //    //    foreach (var channel in guild.Channels)
                //    //    {
                            
                //    //        if (nullify)
                //    //        {
                //    //            //nullify ignored channels
                //    //            var perms = channel.PermissionOverwrites;
                //    //            foreach (var p in perms)
                //    //            {

                //    //                channelPermRestoreTasks.Add(p.TargetType is PermissionTarget.Role
                //    //                ? channel.RemovePermissionOverwriteAsync(
                //    //                    guild.Roles.FirstOrDefault(tr => tr.Id == p.TargetId),
                //    //                    rqParams)
                //    //                : channel.RemovePermissionOverwriteAsync(
                //    //                    guild.Users.FirstOrDefault(tu => tu.Id == p.TargetId),
                //    //                    rqParams)
                //    //                );
                //    //            }
                //    //        }
                //    //        if (channelSelected(channel.Id))
                //    //        {
                //    //            if (!_cSnap.channelData.TryGetValue(channel.Id, out var cdata))
                //    //            {
                //    //                Console.WriteLine($"No data for channel {channel.Name} {channel.Id}! skipping");
                //    //                continue;
                //    //            }
                //    //            //var cdata = _cSnap.channelData[channel.Id];
                //    //            foreach (var pow in cdata.permOverwrites)
                //    //            {
                //    //                if (pow.TargetType is PermissionTarget.Role)
                //    //                {
                //    //                    channelPermRestoreTasks.Add(channel.AddPermissionOverwriteAsync(
                //    //                        guild.Roles.FirstOrDefault(tr => tr.Id == pow.TargetId),
                //    //                        pow.Permissions,
                //    //                        rqParams));
                //    //                }
                //    //                else
                //    //                {
                //    //                    channelPermRestoreTasks.Add(channel.AddPermissionOverwriteAsync(
                //    //                        guild.Users.FirstOrDefault(tu => tu.Id == pow.TargetId),
                //    //                        pow.Permissions,
                //    //                        rqParams));
                //    //                }
                //    //            }
                //    //        }
                //    //        foreach (var t in channelPermRestoreTasks)
                //    //        {
                //    //            try
                //    //            {
                //    //                await t;
                //    //            }
                //    //            catch (Exception e)
                //    //            {
                //    //                Console.WriteLine($"Could not restore perm overwrites:");
                //    //            }
                //    //        }
                //    //        //channel.RemovePermissionOverwriteAsync()
                //    //    }

                //    //endRollback:
                //    //    break;
                //    }

                case "create":
                    {
                        string path
                            = cPromptAny("Enter snapshot path "),
                            password = default;
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
                        if (cPromptBinary("Set password? ")) password = cPromptAny("Enter password (you will not be able to open the database without it later)");
                        var smode = cPromptFlags<SnapshotMode>("Select snapshot mode");
                        props = new(cPromptAny("Comment?"),
                            DateTime.UtcNow,
                            smode,
                            guild.Id);
                        _cSnap = new SnapshotData(path, password, props);
                        Console.WriteLine($"Created snapshot: {_cSnap}");
                        break;
                    }
                case "capture":
                    if (_cSnap is null)
                    {
                        Console.WriteLine("no snapshot to capture to!");
                        break;
                    }
                    guild = allguilds.FirstOrDefault(g => g.Id == _cSnap.props.guildID);
                    if (guild == null)
                    {
                        Console.WriteLine("Not in target guild!");
                        break;
                    }

                    foreach (var role in guild.Roles)
                    {
                        _cSnap.SetRoleData(role.Id, new(role.Color, role.IsHoisted, role.IsMentionable, role.Permissions.RawValue, role.Name));
                    }
                    foreach (var channel in guild.Channels)
                    {
                        _cSnap.SetChannelData(channel.Id, new(channel.Name, channel.GetChannelType(), channel.getCatID(), (channel as ITextChannel)?.Topic, channel.PermissionOverwrites.ToArray()));
                    }
                    //todo: users
                    
                    break;
                case "open":
                    if (_cSnap is not null)
                    {
                        Console.WriteLine("There is a snapshot already opened! ");
                        break;
                    }
                    var name = cPromptAny("Enter snapshot address");
                    //if (Directory.Exists(name))
                    //{
                    //    Console.WriteLine("Folder occupied! ");
                    //    break;
                    //}
                    var pw = cPromptAny("Password? ");
                    try
                    {
                        _cSnap = new SnapshotData(name, pw);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("GENERAL LOAD ERROR: " + e);
                    }
                    break;
                case "close":
                    _cSnap = null;
                    break;
                case "props":
                    props = new(cPromptAny("Comment: "), _cSnap.props.creationDate, cPromptFlags<SnapshotMode>("Select mode"), _cSnap.props.guildID);
                    _cSnap.props = props;
                    break;
                case "send":
                    break;
            }
            if (r is not "exit") goto mainLoop;
            //end main loop
            //_cSnap = null;
            Console.WriteLine("Exiting...");
            exitMark.Release();
        }
    }

}