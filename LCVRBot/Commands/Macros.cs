using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord;
using NetCord.Gateway;
using LCVRBot;
using System.Reflection;
using System.Drawing;

namespace LCVRBot.Commands
{
    public class Macros : ApplicationCommandModule<ApplicationCommandContext>
    {
        //  command for adding a macro      do NOT allow people to use the command in DMs lol       Make sure only admins can use it    only allow LCVR admins to use it
        [SlashCommand("add-macro", "Add a macro to use", Contexts = [InteractionContextType.Guild], DefaultGuildUserPermissions = Permissions.Administrator)]
        public async Task AddMacro(
            [SlashCommandParameter(Name = "name", Description = "The name of the macro. (Don't include the . and no spaces)")] string macroName,
            [SlashCommandParameter(Name = "description", Description = "A quick description of the macro")] string macroDescription,
            [SlashCommandParameter(Name = "macro-text", Description = "What you want the macro to say when used")] string macroText,
            [SlashCommandParameter(Name = "attachment1", Description = "(optional) First attachment that you want to be sent along with the macro")] Attachment? attachment1 = null,
            [SlashCommandParameter(Name = "attachment2", Description = "(optional) Second attachment that you want to be sent along with the macro")] Attachment? attachment2 = null,
            [SlashCommandParameter(Name = "attachment3", Description = "(optional) Third attachment that you want to be sent along with the macro")] Attachment? attachment3 = null)
        {
            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Adding .{macroName} macro..." }));

            try // try-catch adding it in case sumn fails
            {
                // download the attachments to appdataPath, then reupload for non expiring links
                // create a list of saved files
                // and delete the downloaded files
                List<string> files = [];
                if (attachment1 != null)
                {
                    using (var client = new WebClient())
                    {
                        string filePath = Program.appdataPath + attachment1.FileName;
                        client.DownloadFile(attachment1.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                        Stream attachmentStream = File.OpenRead(filePath);
                        RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment1.FileName, attachmentStream)] });
                        files.Add(attachmentMessage.Attachments[0].Url);
                        File.Delete(filePath);
                    }
                }
                if (attachment2 != null)
                {
                    using (var client = new WebClient())
                    {
                        string filePath = Program.appdataPath + attachment2.FileName;
                        client.DownloadFile(attachment2.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                        Stream attachmentStream = File.OpenRead(filePath);
                        RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment2.FileName, attachmentStream)] });
                        files.Add(attachmentMessage.Attachments[0].Url);
                        File.Delete(filePath);
                    }
                }
                if (attachment3 != null)
                {
                    using (var client = new WebClient())
                    {
                        string filePath = Program.appdataPath + attachment3.FileName;
                        client.DownloadFile(attachment3.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                        Stream attachmentStream = File.OpenRead(filePath);
                        RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment3.FileName, attachmentStream)] });
                        files.Add(attachmentMessage.Attachments[0].Url);
                        File.Delete(filePath);
                    }
                }

                // add the macro
                BotSettings.settings.macroList.Add(macroName, (macroDescription, macroText.Replace("\\n", "\n"), files.ToArray()));
                BotSettings.Save();

                await ModifyResponseAsync((props) => { props.Content = $"Added .{macroName} successfully!"; });
                Console.WriteLine($"Added .{macroName} successfully!");
            }
            catch (Exception ex)
            {
                await ModifyResponseAsync((props) => { props.Content = $"Adding macro failed with {ex.Message}"; });
                Console.WriteLine($"Adding macro failed with {ex.Message}");
            }
        }

        // context menu for adding a macro   do NOT allow people to use the command in DMs lol       Make sure only admins can use it    only allow LCVR admins to use it
        [MessageCommand( "add-macro", Contexts = [InteractionContextType.Guild], DefaultGuildUserPermissions = Permissions.Administrator)]
        public async Task AddMacroContext(RestMessage message)
        {
            // split by newline to parse out macro name
            var macroParse = message.Content.Split("\n");
            string? macroName = null;
            string? macroText = null;

            // check the array lenght
            switch (macroParse.Length)
            {
                // if they didn't type anything, and just sent something, invalid
                case 0:
                    await RespondAsync(InteractionCallback.Message(new() { Content = "Invalid macro to add, nothing in message", Flags = MessageFlags.Ephemeral }));
                    return;
                // if they just typed the macro name, it'll likely be just an image macro
                // but do check that it is a macro name
                case 1:
                    macroName = macroParse[0].Remove(0,1);
                    if (macroName.StartsWith('.') && macroName.Split(' ').Length == 1)
                        break;
                    await RespondAsync(InteractionCallback.Message(new() { Content = $"Invalid macro to add, macro name is invalid: {macroName}", Flags = MessageFlags.Ephemeral }));
                    return;
                // else they likely typed out the macro name, plus more to parse for text and description
                // but again make sure of valid macro name
                default:
                    macroName = macroParse[0];
                    if (!(macroName.StartsWith('.') && macroName.Split(' ').Length == 1))
                    {
                        await RespondAsync(InteractionCallback.Message(new() { Content = $"Invalid macro to add, macro name is invalid: {macroName}", Flags = MessageFlags.Ephemeral }));
                        return;
                    }

                    macroText += macroParse[1];
                    for (int i = 2; i < macroParse.Length; i++)
                        macroText += "\n" + macroParse[i];
                    break;
            }

            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Adding {macroName} macro..." }));

            try // try-catch adding it in case sumn fails
            {
                // download the attachments to appdataPath for storage and reupload
                // and create a list of saved files
                List<string> files = [];
                if (message.Attachments != null && message.Attachments.Any()) 
                {
                    foreach (var attachment in message.Attachments)
                    {
                        files.Add(attachment.Url);
                    }
                }

                string? macroDescription = null;
                if (macroParse[1].StartsWith("@description "))
                {
                    macroDescription = macroParse[1].Replace("@description ", "");
                    macroText = macroText?.Replace(macroParse[1], "");
                }

                macroText = macroText?.TrimStart("\n".ToCharArray());

                // add the macro
                BotSettings.settings.macroList.Add(macroName.Remove(0, 1), (macroDescription!, macroText!, files.ToArray()));
                BotSettings.Save();

                await ModifyResponseAsync((props) => { props.Content = $"Added {macroName} successfully!"; });
                Console.WriteLine($"Added {macroName} successfully!");
            }
            catch (Exception ex)
            {
                await ModifyResponseAsync((props) => { props.Content = $"Adding macro failed with {ex.Message}"; });
                Console.WriteLine($"Adding macro failed with {ex.Message}");
            }
        }

        //  command for removing a macro    do NOT allow people to use the command in DMs lol       Make sure only admins can use it    only allow LCVR admins to use it
        [SlashCommand("remove-macro", "Remove a macro", Contexts = [InteractionContextType.Guild], DefaultGuildUserPermissions = Permissions.Administrator)]
        public async Task RemoveMacro(
            [SlashCommandParameter(Name = "name", Description = "The name of the macro. (Don't include the .)")] string macroName)
        {
            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Removing .{macroName} macro..." }));

            try // try-catch removing it in case sumn fails
            {
                // throw if there isn't a macro with that name
                if (!BotSettings.settings.macroList.TryGetValue(macroName, out (string macroDescription, string macroText, string[] attachments) value)) { throw new KeyNotFoundException($"No macro with name {macroName}"); }

                // delete any associated attachments
                foreach (string attachment in value.attachments)
                {
                    File.Delete(Program.appdataPath + attachment);
                }

                // remove the macro
                BotSettings.settings.macroList.Remove(macroName);
                BotSettings.Save();

                await ModifyResponseAsync((props) => { props.Content = $"Removed .{macroName}"; });
                Console.WriteLine($"Removed .{macroName}");
            }
            catch (Exception ex)
            {
                await ModifyResponseAsync((props) => { props.Content = $"Removing macro failed with {ex.Message}"; });
                Console.WriteLine($"Removing macro failed with {ex.Message}");
            }
        }

        // command for seeing the list of macros   do NOT allow people to use the command in DMs lol    Make sure only admins can use it    only allow LCVR admins to use it
        [SlashCommand("macros", "See the list of macros", Contexts = [InteractionContextType.Guild], DefaultGuildUserPermissions = Permissions.Administrator)]
        public async Task SeeMacros()
        {
            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Getting macros..." }));

            Console.WriteLine("Building embeds");

            try // try-catch getting them in case sumn fails
            {
                // since embeds can't have more than 25 fields, check if there are less than 25 macros
                if (BotSettings.settings.macroList.Count < 26)
                {
                    Console.WriteLine("Count less than 25");

                    // create an embed for the list
                    EmbedProperties embed = new() { Title = "Macros:", Description = "Do .macro @user to mention a user with the macro" };

                    // add a field for each macro
                    List<EmbedFieldProperties> fields = [];
                    foreach (string i in BotSettings.settings.macroList.Keys)
                    {
                        fields.Add(new EmbedFieldProperties() { Name = i, Value = BotSettings.settings.macroList[i].macroDescription });
                    }
                    embed.Fields = fields;

                    // display the macros
                    await ModifyResponseAsync((props) => { props.Content = "Here is a list of macros"; props.Embeds = [embed]; });
                }
                else // if there are more than 25, slip it up into chunks
                {
                    Console.WriteLine("Count more than 25");

                    // calculate the required chunks
                    int chunks = (int)Math.Ceiling((decimal)BotSettings.settings.macroList.Count / 25);
                    EmbedProperties[] embeds = new EmbedProperties[chunks];
                    Console.WriteLine($"Making {chunks} embeds");

                    // create each embed
                    for (int i = 0; i < embeds.Length; i++)
                    {
                        // make the embed
                        EmbedProperties embed = new() { Title = i == 0 ? "Macros:" : "Macros cont:" };
                        Console.WriteLine($"Chunk {i}");

                        // calculate how many macros to add to this chunk
                        int count = BotSettings.settings.macroList.Count - (25 * i) > 25 ? 25 : BotSettings.settings.macroList.Count - (25 * i);
                        Console.WriteLine($"Count {count}");

                        // add a field for each macro in this chunk
                        List<EmbedFieldProperties> fields = [];
                        for (int j = 0; j < count; j++)
                        {
                            int index = j + (25 * i);
                            Console.WriteLine($"Macro {index}");
                            fields.Add(new EmbedFieldProperties() { Name = BotSettings.settings.macroList.ElementAt(index).Key, Value = BotSettings.settings.macroList.ElementAt(index).Value.macroDescription });
                        }
                        embed.Fields = fields;

                        // add the chunk to the list
                        embeds[i] = embed;
                    }

                    Console.WriteLine("Done building embeds");

                    // display the macros
                    await ModifyResponseAsync((props) => { props.Content = "Here is a list of macros"; props.Embeds = embeds; });
                }
            }
            catch (Exception ex)
            {
                await ModifyResponseAsync((props) => { props.Content = $"Getting macros failed with {ex.Message}"; });
                Console.WriteLine($"Getting macros failed with {ex.Message}");
            }
        }

        //  command for editing a macro     do NOT allow people to use the command in DMs lol       Make sure only admins can use it    only allow LCVR admins to use it
        [SlashCommand("edit-macro", "Add a macro to use", Contexts = [InteractionContextType.Guild], DefaultGuildUserPermissions = Permissions.Administrator)]
        public async Task EditMacro(
            [SlashCommandParameter(Name = "name", Description = "The name of the macro. (Don't include the .)")] string macroName,
            [SlashCommandParameter(Name = "description", Description = "(optional) A quick description of the macro")] string? macroDescription = null,
            [SlashCommandParameter(Name = "macro-text", Description = "(optional) What you want the macro to say when used")] string? macroText = null,
            [SlashCommandParameter(Name = "attachment1", Description = "(optional) First attachment that you want to be sent along with the macro")] Attachment? attachment1 = null,
            [SlashCommandParameter(Name = "attachment2", Description = "(optional) Second attachment that you want to be sent along with the macro")] Attachment? attachment2 = null,
            [SlashCommandParameter(Name = "attachment3", Description = "(optional) Third attachment that you want to be sent along with the macro")] Attachment? attachment3 = null)
        {
            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Editing .{macroName} macro..." }));

            try // try-catch editing it in case sumn fails
            {
                // throw if there isnt a macro with that name
                if (!BotSettings.settings.macroList.TryGetValue(macroName, out (string macroDescription, string macroText, string[] attachments) value)) { throw new KeyNotFoundException($"No macro with name {macroName}"); }

                // edit the macro, skipping any part not changed, and attachments as they will be added later
                (string macroDescription, string macroText, string[] attachments) editedMacro = (macroDescription ?? value.macroDescription,
                                                             macroText != null ? macroText.Replace("\\n", "\n") : value.macroText,
                                                             value.attachments);

                // download any attachments to be changed or added, and add them to the edited macro
                // and create a list of saved files
                List<string> files = [.. editedMacro.attachments];
                if (attachment1 != null || attachment2 != null || attachment3 != null )
                {
                    // check if any are being replaced
                    if (attachment1 != null)
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                string filePath = Program.appdataPath + attachment1.FileName;
                                client.DownloadFile(attachment1.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                                Stream attachmentStream = File.OpenRead(filePath);
                                RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment1.FileName, attachmentStream)] });
                                files[0] = attachmentMessage.Attachments[0].Url;
                                File.Delete(filePath);
                            }
                        }
                        catch
                        {
                            using (var client = new WebClient())
                            {
                                string filePath = Program.appdataPath + attachment1.FileName;
                                client.DownloadFile(attachment1.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                                Stream attachmentStream = File.OpenRead(filePath);
                                RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment1.FileName, attachmentStream)] });
                                files.Add(attachmentMessage.Attachments[0].Url);
                                File.Delete(filePath);
                            }
                        }
                    }
                    if (attachment2 != null)
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                string filePath = Program.appdataPath + attachment2.FileName;
                                client.DownloadFile(attachment2.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                                Stream attachmentStream = File.OpenRead(filePath);
                                RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment2.FileName, attachmentStream)] });
                                files[1] = attachmentMessage.Attachments[0].Url;
                                File.Delete(filePath);
                            }
                        }
                        catch
                        {
                            using (var client = new WebClient())
                            {
                                string filePath = Program.appdataPath + attachment2.FileName;
                                client.DownloadFile(attachment2.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                                Stream attachmentStream = File.OpenRead(filePath);
                                RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment2.FileName, attachmentStream)] });
                                files.Add(attachmentMessage.Attachments[0].Url);
                                File.Delete(filePath);
                            }
                        }
                    }
                    if (attachment3 != null)
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                string filePath = Program.appdataPath + attachment3.FileName;
                                client.DownloadFile(attachment3.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                                Stream attachmentStream = File.OpenRead(filePath);
                                RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment3.FileName, attachmentStream)] });
                                files[2] = attachmentMessage.Attachments[0].Url;
                                File.Delete(filePath);
                            }
                        }
                        catch
                        {
                            using (var client = new WebClient())
                            {
                                string filePath = Program.appdataPath + attachment3.FileName;
                                client.DownloadFile(attachment3.Url, File.Exists(filePath) ? filePath[..(filePath.LastIndexOf('.'))] + macroName + filePath[(filePath.LastIndexOf('.'))..] : filePath);
                                Stream attachmentStream = File.OpenRead(filePath);
                                RestMessage attachmentMessage = await Program.attachmentChannel!.SendMessageAsync(new() { Attachments = [new AttachmentProperties(attachment3.FileName, attachmentStream)] });
                                files.Add(attachmentMessage.Attachments[0].Url);
                                File.Delete(filePath);
                            }
                        }
                    }

                    // now add the new list 
                    editedMacro.attachments = [.. files];
                }

                // save the edited macro
                BotSettings.settings.macroList[macroName] = editedMacro;
                BotSettings.Save();

                await ModifyResponseAsync((props) => { props.Content = $"Edited .{macroName} successfully!"; });
                Console.WriteLine($"Edited .{macroName} successfully!");
            }
            catch (Exception ex)
            {
                await ModifyResponseAsync((props) => { props.Content = $"Editing macro failed with {ex.Message}"; });
                Console.WriteLine($"Editing macro failed with {ex.Message}");
            }
        }

        // command for changing the name of a macro    do NOT allow people to use the command in DMs lol       Make sure only admins can use it    only allow LCVR admins to use it
        [SlashCommand("change-macro-name", "Change the name a macro", Contexts = [InteractionContextType.Guild], DefaultGuildUserPermissions = Permissions.Administrator)]
        public async Task RemoveMacro(
            [SlashCommandParameter(Name = "oldname", Description = "The current name of the macro. (Don't include the .)")] string macroName,
            [SlashCommandParameter(Name = "newname", Description = "The new name of the macro. (Don't include the .)")] string newName)
        {
            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Changing .{macroName} to .{newName}..." }));

            try // try-catch removing it in case sumn fails
            {
                // throw if there isn't a macro with that name
                if (!BotSettings.settings.macroList.ContainsKey(macroName)) { throw new KeyNotFoundException($"No macro with name {macroName}"); }

                // remove the macro, saving it's contents
                BotSettings.settings.macroList.Remove(macroName, out (string macroDescription, string macroText, string[] attachments) contents);

                // add it back under the new name and save
                BotSettings.settings.macroList.Add(newName, contents);
                BotSettings.Save();

                await ModifyResponseAsync((props) => { props.Content = $"Changed .{macroName} to .{newName}!"; });
                Console.WriteLine($"Changed .{macroName} to .{newName}!");
            }
            catch (Exception ex)
            {
                await ModifyResponseAsync((props) => { props.Content = $"Changing macro name failed with {ex.Message}"; });
                Console.WriteLine($"Changing macro name failed with {ex.Message}");
            }
        }
    }
}
