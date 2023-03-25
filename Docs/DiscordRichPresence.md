# Instructions on how to add Discord Rich Presence in XNA CnCNet client

About Discord Rich Presence
-----------------------------------
Discord Rich Presence (DRP) is a useful feature that shows in the activity of Discord users the status of what application is open on their computer. For C&C players this feature is especially important, because every year the number of players decreases, and with DRP you can keep track of who plays games and mods for the C&C series via CnCNet client.

How to set up DRP for the client
-----------------------------------
0. Create Discord account (obviously)
1. Open Discord developers portal: [https://discord.com/developers/applications](https://discord.com/developers/applications)
2. Click **New Application** button. Type name of your mod, click on "policy" checkbox and click **Create** button
3. In **General Information** tab of your application you can find **Application Id** (yourself or via ctrl+f) which consists of more than 10 digits. Copy it and add into `Resource\ClientDefinitions.ini` file in section `Settings` in key `DiscordAppId`
4. In **Rich Presence** → **Art Assets** tab you need upload client/mod logo and faction's logos via button **Add Image(s)**. Mod logo should be named as `logo` in application assets. Faction's logo, Random and Spectator logo should name as lowercase stings without spaces and apostrophes (they must pass by [RegExp](https://regexr.com) `[a-z]|[0-9]`), e.i.: `Nod Genesis Legion` must have `nodgenesislegion` logo name, `Yuri's Legi0n` must have `yurislegi0n` logo name. After you upload images click the **Save Changes** button and await authorization on server of your changes in application.
5. After about ~5 min launch client
6. ???
7. Profit