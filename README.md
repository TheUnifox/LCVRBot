# Hello!
This is a .macro bot made for assisting in the Lethal Company VR server! But it's pretty generic, only requiring a couple changes to use for yourself on another server!

# Building and Usage
If you want to use this macro bot on your own server there's just a couple things to do!

Change `LCVR_DISCORD` at line 23 in Program.cs to whatever you want the bot's token environment variable to be\
Change `LCVR Discord` at line 37 to whatever you want the bot to be called when starting up\
Change the long number at line 90 to the ID of the server you want it to operate in\
Change line 101 to however you want the bot to respond to being mentioned
Change all occurences of `LCVRDiscord` in BotSettings.cs to where ever you want the settings file to save to

Now, set the system environment variable with the bot token, add a role called `use-macros` to anyone you want to be allowed to use them.\
And finally just build and run!\
(Make sure to invite the bot to the server you want to use it on)
