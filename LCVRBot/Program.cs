using System;
using System.Runtime.InteropServices;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using LCVRBot.Commands;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json.Linq;
using NetCord.Services;

namespace LCVRBot
{
    internal class Program
    {
        // the main server the bot operates in
        public static RestGuild? mainGuild;

        // the bot, handles everything through here
        public static GatewayClient client = new(new BotToken(Environment.GetEnvironmentVariable("LCVR_DISCORD") ?? ""), new GatewayClientConfiguration() { Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent });

        // for registering / commands
        ApplicationCommandService<ApplicationCommandContext> applicationCommandService = new();

        // to handle gracefully closing the bot
        static ConsoleEventDelegate handler = new ConsoleEventDelegate(HandleExitTasks);
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        public static Task Main(string[] args) => new Program().MainAsync();
        public async Task MainAsync()
        {
            Console.WriteLine("\n*** LCVR Discord ***\n");
            client.Log += Log;
            client.MessageCreate += OnMessage;
            client.Ready += ClientReady;
            //client.GuildUserAdd += UserJoined; // for if welcoming is wanted

            // add modules for commands
            applicationCommandService.AddModules(typeof(Program).Assembly);

            SetConsoleCtrlHandler(handler, true); // to close the bot gracefully

            BotSettings.Load(); // load bot settings, like the macro list

            // register the / commands
            await applicationCommandService.CreateCommandsAsync(client.Rest, client.Id);

            client.InteractionCreate += async interaction =>
            {
                if (interaction is not ApplicationCommandInteraction applicationCommandInteraction)
                    return;
                
                var result = await applicationCommandService.ExecuteAsync(new ApplicationCommandContext(applicationCommandInteraction, client));

                if (result is not IFailResult failResult)
                    return;

                try { await interaction.SendResponseAsync(InteractionCallback.Message(new() { Content = failResult.Message, Flags = MessageFlags.Ephemeral })); }
                catch { }
            };

            await client.StartAsync(); // start it up!

            await Task.Delay(-1); // hold this up for good to keep the bot running
        }

        // close the bot gracefully
        static bool HandleExitTasks(int eventType)
        {
            if (eventType == 2)
            {
                Task.WaitAll([client.CloseAsync()]);
            }
            return false;
        }

        public static ValueTask Log(LogMessage message)
        {
            Console.WriteLine(message);
            return ValueTask.CompletedTask;
        }

        public async ValueTask ClientReady(ReadyEventArgs args)
        {
            mainGuild = await client.Rest.GetGuildAsync(1192754217564254238);
            Console.WriteLine("Started!");
        }

        // handle macros!
        public async ValueTask OnMessage(Message message)
        {
            // if it's a reply to a bot message or mentions the bot, do a funny
            if (message.ReferencedMessage?.Author.Id == client.Id || message.MentionedUsers.Where((user) => { return user.Id == client.Id; }).Any())
            {
                Console.WriteLine("OwO");
                await message.AddReactionAsync(new("🖕"));
            }

            // if it's not a macro, ignore
            if (!message.Content.StartsWith('.')) { return; }
                                                                                               // super convoluted way to check if the person using the macro has use-macros role lol
            if (BotSettings.settings.macroList.Keys.Contains(message.Content.Split(" ")[0].Remove(0, 1)) && ((GuildUser)message.Author).GetRoles(mainGuild!).Where((role) => { return role.Name == "use-macros"; }).Any())
            {
                // get the macro for easier use
                (string macroDescription, string macroText, Color macroColor, Attachment? includedImage) macro = BotSettings.settings.macroList[message.Content.Split(" ")[0].Remove(0, 1)];
                
                // create an embed for the macro, in a list bc send message requires a list of them
                EmbedProperties[] embeds = { new() { Color = macro.macroColor, Description = macro.macroText, Image = macro.includedImage != null ? new(macro.includedImage.Url) : null } };
                
                // send the macro and delete the macro message
                TextGuildChannel channel = (TextGuildChannel)await client.Rest.GetChannelAsync(message.ChannelId);
                await channel.SendMessageAsync(new() { Embeds = embeds, Content = message.Content.Split(" ").Length > 1 ? message.Content.Split(" ")[1] : "" });
                await message.DeleteAsync();
            }
        }

        /* in case a welcoming is wanted
        private async ValueTask UserJoined(GuildUser user)
        {
            Console.WriteLine("User joined");
            var dm = await user.GetDMChannelAsync();
            Console.WriteLine("DM channel");
            await dm.SendMessageAsync("Welcome to TEC! I'm Tecie, the convention bot! You can use my / commands for a lot of things! Hope you have fun here!");
            await dm.SendMessageAsync("You can head over to <#1078858956027478066> to read and accept them. Then you'll have access to the rest of the server!");
            Console.WriteLine("Messages sent");
        }
        */
    }
}
