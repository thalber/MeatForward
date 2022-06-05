using Discord.WebSocket;
using Discord;
using Discord.Rest;

using static MeatForward.ConsoleFace;

namespace MeatForward
{
    internal static partial class MFApp
    {
        private const string tokenKey = "MeatForward_TOKEN";
        private static DiscordSocketClient _client;
        //private static InteractionService _is;
        private static SnapshotData _cSnap;
        private static SemaphoreSlim exitMark = new(0, 1);
        private static string _csnapJson 
            => _cSnap is not null 
            ? Newtonsoft.Json.JsonConvert.SerializeObject(_cSnap, Newtonsoft.Json.Formatting.Indented) 
            : "NULL";
        private static string defaultFilepath;

        static async Task<int> Main(string[] args)
        {
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
        public static async void RunMainLoop()
        {
        mainLoop:
            //inloop repeate use vars
            SocketGuild guild;
            SocketGuild[] allguilds = _client.Guilds.ToArray();

            Console.WriteLine();
            Console.WriteLine($"Current snapshot: {_cSnap}");
            var r = cPrompt("Select needed action: ",
                new[] { "snapshot", "rollback", "read", "write", "console", "send", "exit" },
                true);
            //main loop
            switch (r)
            {
                case "snapshot":
                    {
                        SnapshotMode mode;
                        guild = cPrompt("Choose guild", allguilds.ToArray(), true);
                        mode = cPromptFlags<SnapshotMode>("Set snapshot mode");
                        Console.WriteLine(mode);
                        //guild = allguilds.First(g => g.Name == gSelect);
                        SnapshotData snap = new() { smode = mode, guildID = guild.Id };
                        //Environment.GetEnvironmentVariable("", EnvironmentVariableTarget.User)
                        excepts:
                        var blprompts = new[]
                        {
                            ("users", snap.exceptUsers),
                            ("roles", snap.exceptRoles),
                            ("channels", snap.exceptRoles)
                        };
                        foreach (var bl in blprompts)
                        {
                            if (cPromptBinary($"Except some {bl.Item1}?"))
                            {
                                //get names
                                var inputs = cPromptAny($"Input {bl.Item1} IDs or names: ").
                                    Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                foreach (var input in inputs)
                                {
                                    if (ulong.TryParse(input, out ulong res)) bl.Item2.Add(res);
                                    switch (bl.Item1)
                                    {
                                        case "roles": 
                                            foreach (var role in guild.Roles)
                                            {
                                                if (role.Name == input) bl.Item2.Add(role.Id);
                                                //snap.roleData.Add(role.Id, new(default, false, false, ) { };
                                            }

                                            break;
                                        case "users": 
                                            foreach(var user in guild.Users) 
                                                if (user.Username == input) bl.Item2.Add(user.Id);
                                            break;
                                        case "channels":
                                            foreach (var channel in guild.Channels)
                                            {
                                                if (channel.Name == input) bl.Item2.Add(channel.Id);
                                                #warning what the fuck was i doing?
                                                //snap.channelData.Add(channel.Id, new(channel.Name, 
                                                //    channel.GetChannelType() ?? ChannelType.Text, 
                                                //    channel.getCatID(), 
                                                //    (channel as ITextChannel)?.Topic));
                                            }
                                            break;
                                            #warning something else?
                                    }
                                    
                                }
                            }
                        }
                        //roleData:
                        //foreach (var channel in guild.Channels)
                        //{
                        //    snap.channelData.Add(channel.Id, new(channel.Name, channel.GetChannelType(), channel.getCatID(), (channel as ITextChannel)?.Topic));
                        //}
                        //foreach (var role in guild.Roles)
                        //{

                        //}
                        
                    baseRolePerms:
                        //List<(ulong roleid, ulong perms)> 
                        foreach (var role in guild.Roles) snap.roleData.Add(role.Id, 
                            new(role.Color, 
                            role.IsHoisted, 
                            role.IsMentionable, 
                            role.Permissions.RawValue, 
                            role.Name));

                    channelOverrides:
                        if (!mode.HasFlag(SnapshotMode.SaveOverrides)) goto endMakeSnap;
                        foreach (var channel in guild.Channels)
                        {
                            snap.channelData.Add(
                                channel.Id,
                                new SnapshotData.channelVanityData(channel.Name,
                                    channel.GetChannelType() ?? ChannelType.Text,
                                    channel.getCatID(),
                                    (channel as ITextChannel)?.Topic,
                                    channel.PermissionOverwrites.ToArray()));
                            //.PermissionOverwrites.First().
                        }
                        snap.creationDate = DateTime.UtcNow;
                        _cSnap = snap;
                    endMakeSnap: break;
                    }
                case "rollback":
                    {

                        if (_cSnap is null) { Console.WriteLine("No snapshot for rollback!"); goto endRollback; }
                        Console.WriteLine($"Current snapshot is for guild: {_cSnap.guildID}.");
                        guild = allguilds.FirstOrDefault(g => g.Id == _cSnap.guildID);
                        if (guild is null) { Console.WriteLine("Not in target guild! aborting"); goto endRollback; }
                        var md = _cSnap.smode;
                        var mReason = $"Automated rollback to snapshot {_cSnap.creationDate}";
                        RequestOptions rqParams = new() { AuditLogReason = mReason, RetryMode = RetryMode.AlwaysRetry };

                        bool whitelist = md.HasFlag(SnapshotMode.Whitelist),
                            nullify = md.HasFlag(SnapshotMode.ForceNullifyOmitted),
                            overrides = md.HasFlag(SnapshotMode.SaveOverrides),
                            restoreRoleNames = cPromptBinary("Restore role names?"),
                            recreateLostRoles = cPromptBinary("Recreate removed roles?"),
                            restoreChannelNames = cPromptBinary("Restore channel names?"),
                            recreateLostChannels = cPromptBinary("Recreate removed channels?");
                        if (cPromptBinary($"Current mode: {_cSnap.smode}. Update?"))
                            _cSnap.smode = cPromptFlags<SnapshotMode>(null);

                        //recover deleted if needed
                        List<(Task<RestRole> task, ulong oldID)> roleRecreateTasks = new();

                        //recreate roles
                        foreach (var sr in _cSnap.roleData)
                        {
                            var role = guild.Roles.FirstOrDefault(tr => tr.Id == sr.Key);
                            if (role is null && recreateLostRoles) roleRecreateTasks.Add((
                                guild.CreateRoleAsync(sr.Value.name, 
                                color:sr.Value.col, 
                                isHoisted:sr.Value.sep, 
                                isMentionable:sr.Value.ment, 
                                permissions:new(sr.Value.perms),
                                options: rqParams 
                                ), sr.Key));
                        }

                        foreach (var t in roleRecreateTasks)
                        {
                            try
                            {
                                //t.task.Wait();
                                var res = await t.task;//t.task.Result;
                                if (res is null) continue;
                                //update ID on roledata
                                var oldData = _cSnap.roleData[t.oldID];
                                _cSnap.roleData.Remove(t.oldID);
                                _cSnap.roleData.Add(res.Id, oldData);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Failed to restore role: {e}");
                            }
                        }
                        roleRecreateTasks.Clear();
                        //roleRestoreTasks.Clear();

                        RestCategoryChannel plain = default;
                        IChannel plainCastTest = plain;

                        //Task<Discord.Rest.RestTextChannel> generic = default;
                        //Task<IChannel> genericCastTest = generic;

                        //the latter is impossible because generics can't be cast properly
                        //fuck you c#

                        List<(Task task, ulong oldId)> channelRecreateTasks = new();
                        //restore channels
                        foreach (var sc in _cSnap.channelData)
                        {
                            var channel = guild.Channels.FirstOrDefault(tc => tc.Id == sc.Key);
                            if (channel is null && recreateLostChannels) switch (sc.Value.type)
                                {
                                    case ChannelType.Text:
                                        channelRecreateTasks.Add(
                                            (guild.CreateTextChannelAsync(sc.Value.name,
                                            ntc => { ntc.Name = sc.Value.name; 
                                                ntc.CategoryId = sc.Value.categoryId; 
                                                ntc.Topic = sc.Value.topic ?? String.Empty; },
                                            rqParams), 
                                            sc.Key)
                                            ); 
                                        break;
                                    case ChannelType.Voice:
                                        channelRecreateTasks.Add(
                                            (guild.CreateVoiceChannelAsync(sc.Value.name,
                                            nvc => { nvc.Name = sc.Value.name; nvc.CategoryId = sc.Value.categoryId; },
                                            rqParams), sc.Key)
                                            );
                                        break;
                                    case ChannelType.Category:
                                        channelRecreateTasks.Add(
                                            (guild.CreateCategoryChannelAsync(sc.Value.name,
                                            ncc => { ncc.Name = sc.Value.name; }, 
                                            rqParams), sc.Key)
                                            );
                                        break;
                                    default:
                                        Console.WriteLine();
                                        break;

                                }
                        }
                        foreach (var t in channelRecreateTasks)
                        {
                            try
                            {
                                await t.task;
                                //var res = t.Res;
                                //if (t is Task<Discord.Rest.>)
                                //dynamic res;
                                var t_tc = t.task as Task<RestTextChannel>;
                                var t_vc = t.task as Task<RestVoiceChannel>;
                                var t_cc = t.task as Task<RestCategoryChannel>;
                                IChannel res = t_tc?.Result ?? t_vc?.Result ?? t_cc?.Result as IChannel;
                                if (res is null) continue;
                                var oldData = _cSnap.channelData[t.oldId];
                                _cSnap.channelData.Remove(t.oldId);
                                _cSnap.channelData.Add(res.Id, oldData);
                                //res ??= t_cc?.Result;
                                //var oldData = _cSnap.channelData.
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Failed to recreate channel: {e}");
                            }
                        }
                        List<Task> rolePermRestoreTasks = new();

                        setRolePerms:
                        bool roleSelected(ulong r) 
                            => _cSnap.exceptRoles.Contains(r) ^ !whitelist;
                        bool userSelected(ulong u) 
                            => _cSnap.exceptUsers.Contains(u) ^ !whitelist;
                        bool channelSelected(ulong c)
                            => _cSnap.exceptChannels.Contains(c) ^ !whitelist;

                        foreach (var role in guild.Roles)
                        {
                            if (roleSelected(role.Id)) rolePermRestoreTasks.Add( role.ModifyAsync(rp => { 
                                rp.Permissions = new(new(_cSnap.roleData[role.Id].perms)); },
                                options:rqParams));
                            else if (nullify)
                            {
                                rolePermRestoreTasks.Add(role.ModifyAsync(rp => 
                                rp.Permissions = new(new(0)),
                                options:rqParams));
                            }
                        }

                        foreach (var t in rolePermRestoreTasks)
                        {
                            try
                            {
                                await t;
                            }
                            catch (Exception e) { Console.WriteLine($"Could not restore role perms: {e}"); }
                        }

                        _client.PurgeDMChannelCache();

                        setChannelOverrides:
                        List<Task> channelPermRestoreTasks = new();
                        foreach (var channel in guild.Channels)
                        {
                            
                            if (nullify)
                            {
                                //nullify ignored channels
                                var perms = channel.PermissionOverwrites;
                                foreach (var p in perms)
                                {

                                    channelPermRestoreTasks.Add(p.TargetType is PermissionTarget.Role
                                    ? channel.RemovePermissionOverwriteAsync(
                                        guild.Roles.FirstOrDefault(tr => tr.Id == p.TargetId),
                                        rqParams)
                                    : channel.RemovePermissionOverwriteAsync(
                                        guild.Users.FirstOrDefault(tu => tu.Id == p.TargetId),
                                        rqParams)
                                    );
                                }
                            }
                            if (channelSelected(channel.Id))
                            {
                                if (!_cSnap.channelData.TryGetValue(channel.Id, out var cdata))
                                {
                                    Console.WriteLine($"No data for channel {channel.Name} {channel.Id}! skipping");
                                    continue;
                                }
                                //var cdata = _cSnap.channelData[channel.Id];
                                foreach (var pow in cdata.permOverwrites)
                                {
                                    if (pow.TargetType is PermissionTarget.Role)
                                    {
                                        channelPermRestoreTasks.Add(channel.AddPermissionOverwriteAsync(
                                            guild.Roles.FirstOrDefault(tr => tr.Id == pow.TargetId),
                                            pow.Permissions,
                                            rqParams));
                                    }
                                    else
                                    {
                                        channelPermRestoreTasks.Add(channel.AddPermissionOverwriteAsync(
                                            guild.Users.FirstOrDefault(tu => tu.Id == pow.TargetId),
                                            pow.Permissions,
                                            rqParams));
                                    }
                                }
                            }
                            foreach (var t in channelPermRestoreTasks)
                            {
                                try
                                {
                                    await t;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Could not restore perm overwrites:");
                                }
                            }
                            //channel.RemovePermissionOverwriteAsync()
                        }

                    endRollback:
                        break;
                    }
                case "console":
                    {
                        Console.WriteLine(_csnapJson);
                        break;
                    }
                case "write":
                    {
                        try
                        {
                            var tpath = cPromptAny("input filename: >");
                            File.WriteAllText(cPromptAny("input filename: >"), _csnapJson);
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                        break;
                    }
                case "read":
                    {
                        try
                        {
                            _cSnap = Newtonsoft.Json.JsonConvert.DeserializeObject<SnapshotData>(File.ReadAllText(cPromptAny("input filename: >")));
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                        break;
                    }
                case "send":
                    {
                        guild = cPrompt("Choose guild", allguilds, true);
                        //guild = allguilds.First(g => g.Name == gSelect);
                        //var allChannels = guild.Channels.ToArray();
                        //cSelect = cPrompt("Choose channel", guild.Channels.ToArray(), true);
                        var channel = cPrompt("Choose channel", guild.Channels.SkipWhile(xx => xx is not ITextChannel).ToArray(), true);
                        if (channel is ITextChannel tc) tc.SendMessageAsync($"```json\n{_csnapJson}\n```");
                        else
                        {
                            Console.WriteLine("Not a text channel!");
                        }
                        //#warning finish;
                        break;
                    }
            }
            if (r is not "exit") goto mainLoop;
            //end main loop
            Console.WriteLine("Exiting...");
            exitMark.Release();
        }
        
    }
}