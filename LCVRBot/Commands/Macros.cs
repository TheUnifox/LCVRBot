using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        [SlashCommand("add-macro", "Add a macro to use", Contexts = [InteractionContextType.Guild])]
        public async Task AddMacro(
            [SlashCommandParameter(Name = "name", Description = "The name of the macro. (Don't include the . and no spaces)")] string macroName,
            [SlashCommandParameter(Name = "description", Description = "A quick description of the macro")] string macroDescription,
            [SlashCommandParameter(Name = "macro-text", Description = "What you want the macro to say when used")] string macroText,
            [SlashCommandParameter(Name = "color", Description = "the color of the macro embed in #RRGGBB format")] string macroColor,
            [SlashCommandParameter(Name = "images", Description = "Images that you want to be sent along with the macro")] Attachment? includedImage = null)
        {
            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Adding .{macroName} macro..." }));

            try // try-catch adding it in case sumn fails
            {
                // add the macro
                BotSettings.settings.macroList.Add(macroName, (macroDescription, macroText, new NetCord.Color(ColorTranslator.FromHtml(macroColor).ToArgb()), includedImage?.Url));
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
                if (!BotSettings.settings.macroList.ContainsKey(macroName)) { throw new KeyNotFoundException($"No macro with name {macroName}"); }

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
            [SlashCommandParameter(Name = "color", Description = "(optional) the color of the macro embed in #rrggbb format")] string? macroColor = null,
            [SlashCommandParameter(Name = "images", Description = "(optional) Images that you want to be sent along with the macro")] Attachment? includedImage = null)
        {
            // respond immediately to avoid timeout
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Editing .{macroName} macro..." }));

            try // try-catch editing it in case sumn fails
            {
                // throw if there isnt a macro with that name
                if (!BotSettings.settings.macroList.ContainsKey(macroName)) { throw new KeyNotFoundException($"No macro with name {macroName}"); }

                // edit the macro, skipping any part not changed
                BotSettings.settings.macroList[macroName] = (macroDescription ?? BotSettings.settings.macroList[macroName].macroDescription,
                                                             macroText ?? BotSettings.settings.macroList[macroName].macroText,
                                                             macroColor != null ? new NetCord.Color(ColorTranslator.FromHtml(macroColor).ToArgb()) : BotSettings.settings.macroList[macroName].macroColor,
                                                             includedImage != null ? includedImage.Url : BotSettings.settings.macroList[macroName].includedImage);
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
                (string macroDescription, string macroText, NetCord.Color macroColor, string? includedImage) contents;
                BotSettings.settings.macroList.Remove(macroName, out contents);

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
