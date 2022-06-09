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
                    }
                    //todo: users                    
                    break;
                case "rollback":
                    {

                    }
                    break;
                case "open":
                    if (_cSnap is not null)
                    {
                        Console.WriteLine("There is a snapshot already opened! ");
                        break;
                    }
                    var name = cPromptAny("Enter snapshot address");
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