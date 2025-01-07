using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using LCVRBot.Commands;
using NetCord.Services;
using System.Drawing;

namespace LCVRBot
{
    internal class Program
    {
        // the main server the bot operates in
        public static RestGuild? mainGuild;

        // a channel to upload attachments to for nicer linking
        public static TextGuildChannel? attachmentChannel;

        public static string appdataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCVRDiscord");

        // the bot, handles everything through here
        public static GatewayClient client = new(new BotToken(Environment.GetEnvironmentVariable("LCVR_DISCORD") ?? ""), new GatewayClientConfiguration() { Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent });

        // for registering / commands
        readonly ApplicationCommandService<ApplicationCommandContext> applicationCommandService = new();

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

            var cancellationToken = new CancellationTokenSource();

            Console.CancelKeyPress += (_, args) =>
            {
                args.Cancel = true;

                client.CloseAsync().Wait();
                cancellationToken.Cancel();
            };

            BotSettings.Load(); // load bot settings, like the macro list

            // register the / commands
            await applicationCommandService.CreateCommandsAsync(client.Rest, client.Id);

            client.InteractionCreate += async interaction =>
            {
                if (interaction is not ApplicationCommandInteraction applicationCommandInteraction)
                    return;

                var result =
                    await applicationCommandService.ExecuteAsync(
                        new ApplicationCommandContext(applicationCommandInteraction, client));

                if (result is not IFailResult failResult)
                    return;

                try
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message(new()
                        { Content = failResult.Message, Flags = MessageFlags.Ephemeral }));
                }
                catch
                {
                }
            };

            try
            {
                await client.StartAsync(cancellationToken: cancellationToken.Token); // start it up!
                await Task.Delay(Timeout.Infinite,
                    cancellationToken.Token); // hold this up for good to keep the bot running
            }
            catch (TaskCanceledException)
            {
            }
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
            mainGuild = await client.Rest.GetGuildAsync(ulong.Parse(Environment.GetEnvironmentVariable("LCVR_GUILD") ?? ""));
            attachmentChannel = (TextGuildChannel)await client.Rest.GetChannelAsync(ulong.Parse(Environment.GetEnvironmentVariable("LCVR_ATT_CHANNEL") ?? ""));
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
                // get a list of parameters to insert, removing the macro from the list
                List<string> macroParams = [.. message.Content.Split(" ")];
                string macroName = macroParams[0].Remove(0, 1);
                macroParams.RemoveAt(0);

                // get the macro for easier use
                (string macroDescription, string macroText, string[] attachments) macro = BotSettings.settings.macroList[macroName];
                List<string>? macroAttachments = macro.attachments != null ? [..macro.attachments] : null;
                string macroText = macro.macroText;

                // replace {int} params
                // replace {0} with nothing if nothing is specified tho
                if (macroParams.Count == 0) { macroText = macroText.Replace("{0}", ""); }
                for (int paramNum = 0; paramNum < macroParams.Count; paramNum++)
                {
                    if (paramNum != macroParams.Count - 1 && !macroText.Contains($"{{{paramNum}}}"))
                    {
                        string paramRemain = "";
                        for (int i = paramNum; i < macroParams.Count; i++) { paramRemain += macroParams[i]; }
                        macroText = macroText.Replace($"{{{paramNum}}}", paramRemain);
                        break;
                    }
                    macroText = macroText.Replace($"{{{paramNum}}}", macroParams[paramNum]);
                }

                // check if it's not an embed
                if (!macroText.StartsWith("!embed"))
                {
                    // if it's not, replace {att(int)} params
                    // and add extra attachments at the end
                    if (macroAttachments != null && macroAttachments.Count > 0)
                    {
                        for (int paramNum = 0; paramNum < macroAttachments.Count; paramNum++)
                        {
                            if (!macroText.Contains($"{{att{paramNum}}}"))
                            {
                                macroText += "\n" + macroAttachments[paramNum];
                                continue;
                            }
                            macroText = macroText.Replace($"{{att{paramNum}}}", macroAttachments[paramNum]);
                        }
                    }

                    // and send as message
                    TextGuildChannel messageChannel = (TextGuildChannel)await client.Rest.GetChannelAsync(message.ChannelId);
                    await messageChannel.SendMessageAsync(new() { Content = macroText });
                    await message.DeleteAsync();

                    return;
                }

                // check for an image attachment to set as embed image
                string? embedImage = null;
                if (macroAttachments != null && macroAttachments.Count != 0)
                {
                    int attIndex = 0;
                    foreach (var attachment in macroAttachments)
                    {
                        if (attachment.Contains(".jpg") || attachment.Contains(".png") || attachment.Contains(".gif"))
                            { embedImage = attachment; macroAttachments.RemoveAt(attIndex); break; }
                        attIndex++;
                    }
                }

                // replace {att(int)} params
                // and add extra attachments at the end
                if (macroAttachments != null && macroAttachments.Count != 0)
                {
                    for (int paramNum = 0; paramNum < macroAttachments.Count; paramNum++)
                    {
                        if (!macroText.Contains($"{{att{paramNum}}}"))
                        {
                            macroText += "\n" + macroAttachments[paramNum];
                            continue;
                        }
                        macroText = macroText.Replace($"{{att{paramNum}}}", macroAttachments[paramNum]);
                    }
                }

                // get ready to parse the rest for embed
                List<string> textParts = [.. macroText.Split("\n")];
                string? embedTitle = null;
                string? embedFooter = null;
                NetCord.Color? embedColor = null;
                string embedText = "";

                // check for embed title and colour
                for (int i = 0; i < textParts.Count; i++)
                {
                    if (textParts[i].StartsWith("!embed"))  { continue; }
                    if (textParts[i].StartsWith("!title ")) { embedTitle = textParts[i].Replace("!title ", ""); continue; }
                    if (textParts[i].StartsWith("!footer ")) { embedFooter = textParts[i].Replace("!footer ", ""); continue; }
                    if (textParts[i].StartsWith("!color"))  { embedColor = new NetCord.Color(ColorTranslator.FromHtml(textParts[i].Replace("!color ", "")).ToArgb()); continue; }
                    embedText += textParts[i] + "\n";
                }

                // send as an embed                                       just a random colour if it wasnt set
                EmbedProperties[] embeds = [new() { Color = embedColor ?? new NetCord.Color(new Random().Next()), Title = embedTitle ?? null, Footer = embedFooter != null ? new() { Text = embedFooter } : null, Image = embedImage ?? null, Description = embedText.Length != 0 ? embedText : null }];
                TextGuildChannel channel = (TextGuildChannel)await client.Rest.GetChannelAsync(message.ChannelId);
                var result = await channel.SendMessageAsync(new() { Embeds = embeds });
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
