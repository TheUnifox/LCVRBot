# Hello!
This is a .macro bot made for assisting in the Lethal Company VR server! But it's pretty generic, only requiring a couple changes to use for yourself on another server!

# Building and Usage
If you want to use this macro bot on your own server there's just a couple things to do!

Change `LCVR_DISCORD` at line 22 in [Program.cs](LCVRBot/Program.cs) to whatever you want the bot's token environment variable to be\
Change `LCVR_GUILD` at line 96 to whatever you want the bot's main guild ID environment variable to be\
Change `LCVR_ATT_CHANNEL` at line 97 to whatever you want the bot's attachment channel ID environment variable to be\
Change `LCVR Discord` at line 31 to whatever you want the bot to be called when starting up\
Change line 108 to however you want the bot to respond to being mentioned\
Change all occurences of `LCVRDiscord` in [BotSettings.cs](LCVRBot/Commands/BotSettings.cs) to where ever you want the settings file to save to

Now, set the system environment variables with the bot token, Guild ID it'll be running in, Channel ID it'll reupload attachments to, add a role called `use-macros` to anyone you want to be allowed to use them.\
And finally just build and run!\
(Make sure to invite the bot to the server you want to use it on)
