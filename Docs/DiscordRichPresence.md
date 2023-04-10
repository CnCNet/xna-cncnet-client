# Instructions on how to add Discord Rich Presence in XNA CnCNet client

About Discord Rich Presence
-----------------------------------
Discord Rich Presence (DRP) is a useful feature that allows Discord users to see what applications are open on their computer. For C&C players, this feature is especially important as the number of players decreases every year, and with DRP you can keep track of who is playing games and mods for the C&C series via the CnCNet client.

How to set up DRP for the client
-----------------------------------
> **Note**
> You are required to be logged in a Discord account.
1. Open Discord developers portal: [https://discord.com/developers/applications](https://discord.com/developers/applications).
2. Click **New Application** button. Type name of your mod, click on "policy" checkbox and click **Create** button.
3. In **General Information** tab of your application you can find **Application Id** (yourself or via ctrl+f) which consists of more than 10 digits. Copy it and add into `Resource\ClientDefinitions.ini` file in section `Settings` in key `DiscordAppId`.
![opera_yNmIcjiUfo](https://user-images.githubusercontent.com/61310813/230958472-efb8bcb1-332b-428b-b9d1-e029296cdb27.png)
5. In **Rich Presence** → **Art Assets** tab you need upload client/mod logo and faction's logos via button **Add Image(s)**. Mod logo should be named as `logo` in application assets. Faction's logo, Random and Spectator logo should name as lowercase stings without spaces and apostrophes (they must pass by [RegExp](https://regexr.com) `[a-z]|[0-9]`), e.i.: `Nod Genesis Legion` must have `nodgenesislegion` logo name, `Yuri's Legi0n` must have `yurislegi0n` logo name. After you upload images click the **Save Changes** button and await authorization on server of your changes in application.
![opera_XjJubOfW5c](https://user-images.githubusercontent.com/61310813/230959370-7bf16984-cf4d-4776-b036-2f9e21239a2a.png)
7. Launch client after ~5 min.

Result: 

https://user-images.githubusercontent.com/61310813/230956028-815a5539-8fb8-43dd-9134-4033a9dcc049.mp4

