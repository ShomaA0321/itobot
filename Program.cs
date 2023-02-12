using System.Text;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace itobot {
    class Program {
        readonly StringBuilder sb;
        readonly CommandService cs;
        readonly DiscordSocketClient client;
        readonly IServiceProvider isp;

        Ito? ito;
        readonly List<ulong> players;

        Program() {
            sb = new();
            cs = new();
            client = new(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.None });
            isp = new ServiceCollection().BuildServiceProvider();
            players = new();
        }

        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync() {
            await cs.AddModulesAsync(Assembly.GetEntryAssembly(), isp);
            client.Log += Log;
            client.Ready += Ready;
            client.SlashCommandExecuted += SlashCommandExecuted;
            await client.LoginAsync(TokenType.Bot, Config.Bot.BotToken);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        public Task Log(LogMessage msg) {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task Ready() {
            List<string> cmds = (await client.Rest.GetGuildApplicationCommands(Config.ID.ServerID)).Select(s => s.Name).ToList();

            SlashCommandBuilder ito = new SlashCommandBuilder()
                .WithName("ito")
                .WithDescription("Play ito")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("player")
                    .WithDescription("Edit player")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("join")
                        .WithDescription("Add player")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("user", ApplicationCommandOptionType.User, "User", isRequired: false))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("leave")
                        .WithDescription("Remove player")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("user", ApplicationCommandOptionType.User, "User", isRequired: false))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("list")
                        .WithDescription("List players")
                        .WithType(ApplicationCommandOptionType.SubCommand)))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("start")
                    .WithDescription("Start ito")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("finish")
                    .WithDescription("Finish ito")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("check")
                    .WithDescription("Check number")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("submit")
                    .WithDescription("Submit number")
                    .WithType(ApplicationCommandOptionType.SubCommand));
                await client.Rest.CreateGuildCommand(ito.Build(), Config.ID.ServerID);

        }

        private async Task SlashCommandExecuted(SocketSlashCommand cmd) {
            string arg0 = cmd.Data.Name;
            if (arg0 == "ito") {
                SocketSlashCommandDataOption arg1 = cmd.Data.Options.First();
                //ゲーム中
                if (ito != null) {
                    switch (arg1.Name) {
                        case "player":
                            SocketSlashCommandDataOption arg2 = arg1.Options.First();
                            SocketGuildUser arg3;
                            if (arg2.Options.Count == 0) arg3 = (SocketGuildUser)cmd.User;
                            else arg3 = (SocketGuildUser)arg2.Options.First().Value;
                            switch (arg2.Name) {
                                case "leave":
                                    if (players.Contains(arg3.Id)) {
                                        players.Remove(arg3.Id);
                                        ito.Leave(arg3.Id);
                                        string name = client.GetGuild(Config.ID.ServerID).GetUser(arg3.Id).DisplayName;
                                        if (name.Length > 10) name = string.Concat(name.AsSpan(0, 10), "...");
                                        await cmd.RespondAsync($"退室 : {name}");
                                    }
                                    break;
                                case "list":
                                    sb.Clear();
                                    sb.AppendLine("参加リスト");
                                    foreach (ulong uid in players) {
                                        string name = client.GetGuild(Config.ID.ServerID).GetUser(uid).DisplayName;
                                        if (name.Length > 10) sb.AppendLine(string.Concat(name.AsSpan(0, 10), "..."));
                                        else sb.AppendLine(name);
                                    }
                                    await cmd.RespondAsync(sb.ToString());
                                    break;
                            }
                            break;
                        case "finish":
                            List<(bool, byte, ulong)> res = ito.Result();
                            ito.Dispose();
                            ito = null;
                            sb.Clear();
                            sb.AppendLine("ゲーム終了");
                            foreach ((bool, byte, ulong) b in res) {
                                sb.Append(b.Item1 ? "O" : "X");
                                sb.Append("  ");
                                sb.Append($"{b.Item2, 3}");
                                sb.Append("  ");
                                string name = client.GetGuild(Config.ID.ServerID).GetUser(b.Item3).DisplayName;
                                if (name.Length > 10) sb.AppendLine(string.Concat(name.AsSpan(0, 10), "..."));
                                else sb.AppendLine(name);
                            }
                            sb.AppendLine($"不正解数{players.Count - res.Count(c => c.Item1)}");
                            await cmd.RespondAsync(sb.ToString());
                            break;
                        case "check":
                            await cmd.RespondAsync(ito.Check(cmd.User.Id).ToString(), ephemeral: true);
                            break;
                        case "submit":
                            List<ulong> sub = ito.Submit(cmd.User.Id);
                            sb.Clear();
                            sb.AppendLine("現在の並び");
                            foreach (ulong uid in sub) {
                                string name = client.GetGuild(Config.ID.ServerID).GetUser(uid).DisplayName;
                                if (name.Length > 10) sb.AppendLine(string.Concat(name.AsSpan(0, 10), "..."));
                                else sb.AppendLine(name);
                            }
                            await cmd.RespondAsync(sb.ToString());
                            break;
                    }
                }
                //ゲーム外
                else {
                    switch (arg1.Name) {
                        case "player":
                            SocketSlashCommandDataOption arg2 = arg1.Options.First();
                            SocketGuildUser arg3;
                            if (arg2.Options.Count == 0) arg3 = (SocketGuildUser)cmd.User;
                            else arg3 = (SocketGuildUser)arg2.Options.First().Value;
                            switch (arg2.Name) {
                                case "join":
                                    if (!players.Contains(arg3.Id)) {
                                        players.Add(arg3.Id);
                                        string name = client.GetGuild(Config.ID.ServerID).GetUser(arg3.Id).DisplayName;
                                        if (name.Length > 10) name = string.Concat(name.AsSpan(0, 10), "...");
                                        await cmd.RespondAsync($"入室 : {name}");
                                    }
                                    break;
                                case "leave":
                                    if (players.Contains(arg3.Id)) {
                                        players.Remove(arg3.Id);
                                        string name = client.GetGuild(Config.ID.ServerID).GetUser(arg3.Id).DisplayName;
                                        if (name.Length > 10) name = string.Concat(name.AsSpan(0, 10), "...");
                                        await cmd.RespondAsync($"退室 : {name}");
                                    }
                                    break;
                                case "list":
                                    sb.Clear();
                                    sb.AppendLine("参加リスト");
                                    foreach (ulong uid in players) {
                                        string name = client.GetGuild(Config.ID.ServerID).GetUser(uid).DisplayName;
                                        if (name.Length > 10) sb.AppendLine(string.Concat(name.AsSpan(0, 10), "..."));
                                        else sb.AppendLine(name);
                                    }
                                    await cmd.RespondAsync(sb.ToString());
                                    break;
                            }
                            break;
                        case "start":
                            ito = new(players);
                            await cmd.RespondAsync("ゲーム開始");
                            break;
                    }
                }
            }
        }
    }
}