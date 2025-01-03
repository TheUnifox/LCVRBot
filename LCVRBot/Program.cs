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
using System.Linq;

namespace LCVRBot
{
    internal class Program
    {
        // the main server the bot operates in
        public static RestGuild? mainGuild;

        // a channel to upload attachments to for nicer linking
        public static TextGuildChannel? attachmentChannel;

        public static string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LCVRDiscord\\";

        // the bot, handles everything through here
        public static GatewayClient client = new(new BotToken(Environment.GetEnvironmentVariable("LCVR_DISCORD") ?? ""), new GatewayClientConfiguration() { Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent });

        // for registering / commands
        readonly ApplicationCommandService<ApplicationCommandContext> applicationCommandService = new();

        // to handle gracefully closing the bot
        static readonly ConsoleEventDelegate handler = new(HandleExitTasks);
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        public static Task Main() => new Program().MainAsync();
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
            attachmentChannel = (TextGuildChannel)await client.Rest.GetChannelAsync(1324573814947975220);
            Console.WriteLine("Started!");
        }

        // handle macros!
        public async ValueTask OnMessage(Message message)
        {
            // if it's a reply to a bot message or mentions the bot, do a funny
            if (message.ReferencedMessage?.Author.Id == client.Id || message.MentionedUsers.Where((user) => { return user.Id == client.Id; }).Any())
            {
                Console.WriteLine("OwO");
                await message.AddReactionAsync(new("🖕")); // maybe change to sumn LCVR specific later
            }

            // if it's not a macro, ignore
            if (!message.Content.StartsWith('.')) { return; }
                                                                                               // super convoluted way to check if the person using the macro has use-macros role lol
            if (BotSettings.settings.macroList.ContainsKey(message.Content.Split(" ")[0].Remove(0, 1)) && ((GuildUser)message.Author).GetRoles(mainGuild!).Where((role) => { return role.Name == "use-macros"; }).Any())
            {
                // get the macro for easier use
                (string macroDescription, string macroText, Color macroColor, string[] attachments) macro = BotSettings.settings.macroList[message.Content.Split(" ")[0].Remove(0, 1)];
                List<string>? macroAttachments = macro.attachments != null ? [..macro.attachments] : null;

                // select an image from the attachments to embed
                // and add attachments nicely to the end of the message, instead of leaving them at the top
                string? embedImage = null;
                string macroTextWAttach = macro.macroText;
                if (macroAttachments != null && macroAttachments.Count != 0)
                {
                    if (macroAttachments.Count > 0 && (macroAttachments[0].EndsWith(".jpg") || macroAttachments[0].EndsWith(".png") || macroAttachments[0].EndsWith(".gif"))) { embedImage = macroAttachments[0]; macroAttachments.RemoveAt(0); }
                    else if (macroAttachments.Count > 1 && (macroAttachments[1].EndsWith(".jpg") || macroAttachments[1].EndsWith(".png") || macroAttachments[1].EndsWith(".gif"))) { embedImage = macroAttachments[1]; macroAttachments.RemoveAt(1); }
                    else if (macroAttachments.Count > 2 && (macroAttachments[2].EndsWith(".jpg") || macroAttachments[2].EndsWith(".png") || macroAttachments[2].EndsWith(".gif"))) { embedImage = macroAttachments[2]; macroAttachments.RemoveAt(2); }
                    
                    macroTextWAttach += "\n\n";

                    foreach (string attachment in macroAttachments)
                    {
                        Stream attachmentStream = File.OpenRead(appdataPath + attachment);
                        RestMessage attachmentMessage = await attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment, attachmentStream)] });
                        macroTextWAttach += $"[{attachment}]({attachmentMessage.Attachments[0].Url})\n";
                    }
                }

                // create an embed for the macro, in a list bc send message requires a list of them
                EmbedProperties[] embeds = [new() { Color = macro.macroColor, Description = macroTextWAttach, Image = embedImage != null ? $"attachment://{embedImage}" : null }];
                
                // send the macro and delete the macro message
                TextGuildChannel channel = (TextGuildChannel)await client.Rest.GetChannelAsync(message.ChannelId);

                List<AttachmentProperties> attachments = [];
                if (embedImage != null) 
                {
                    Stream attachmentStream = File.OpenRead(appdataPath + embedImage);
                    attachments.Add(new AttachmentProperties(embedImage, attachmentStream));
                }

                await channel.SendMessageAsync(new() { Embeds = embeds, Content = message.Content.Split(" ").Length > 1 ? message.Content.Split(" ")[1] : "", Attachments = attachments.Any() ? attachments : null });
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
