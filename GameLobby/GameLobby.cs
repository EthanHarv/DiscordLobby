using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordLobby
{
    class GameLobby
    {
        public struct ListItems
        {
            public ulong GuildId, CategoryId, OwnerId;
            public string Game, Bio, Access, Key;

            public ListItems(ulong LGuildId, ulong LCategoryId, ulong LOwnerId, string LGame, string LBio, string LAccess, string LKey)
            {
                GuildId = LGuildId;
                CategoryId = LCategoryId;
                OwnerId = LOwnerId;
                Game = LGame;
                Bio = LBio;
                Access = LAccess;
                Key = LKey;
            }
        }

        public static List<ListItems> List = new List<ListItems>(); // ListItems = { ulong GuildId, ulong CategoryId, ulong OwnerId, string Game, string Bio, string Access, string Key }

        public static List<ulong> OwnsLobby = new List<ulong>();



        #region Lobby administration.
        public static async Task NewLobby(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true)
        {
            if (DeleteMessage) await message.DeleteAsync();
            if (OwnsLobby.Contains(message.Author.Id))
            {
                var del = await message.Channel.SendMessageAsync("You already own a lobby somewhere! Please do `deletelobby:` in the " +
                    "lobby channel before you make another one."); await Task.Delay(5000); await del.DeleteAsync(); return;
            }
            if (message.Content.Split(' ').Length < 2)
            {
                var del = await message.Channel.SendMessageAsync("Please put a game. (Spaces not allowed in game name.)"); await Task.Delay(8500); await del.DeleteAsync(); return;
            }

            var guild = (message.Author as SocketGuildUser).Guild;

            ListItems finalOutput = new ListItems
            {
                GuildId = guild.Id,
                OwnerId = message.Author.Id
            };
            // {ServerId[0], CateoryId[1], OwnerId[2], Game Name[3], Bio[4], Access[5], Key[6] }

            // Check syntax - Correct syntax = newlobby: {game} {public/private} {200 char max bio}

            string game = message.Content.Split(' ')[1]; // Game
            finalOutput.Game = game;
            if (game.Length >= 80) { var del = await message.Channel.SendMessageAsync($"Game name is 80 characters max. \nWhat game even has such a long name?"); await Task.Delay(5000); await del.DeleteAsync(); return; }
            if (game.Contains("text") || game.Contains("voice")) { var del = await message.Channel.SendMessageAsync("Sorry, but games cannot contain the phrase " + game.ToLower() + "."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            string access = "public";
            if (message.Content.Split(' ').Length > 2)
            {
                access = message.Content.Split(' ')[2]; // Public/Private
                if (!(access.ToLower() == "private" || access.ToLower() == "public")) // Makes sure syntax is correct.
                {
                    var del = await message.Channel.SendMessageAsync("Something went wrong. \nPlease do not use spaces in game names (I.e don't do Rainbow 6, instead do Rainbow6)," +
                        "\nand please say if the lobby is public or private before putting your lobby bio. " +
                        "\n(For instance, don't do `newlobby: Rainbow6 Play some games`, and instead do `newlobby: Rainbow6 Public Play some games`)"); await Task.Delay(12000); await del.DeleteAsync(); return;
                }
                if (message.Content.Split(' ').Length > 3) // Check if bio exists. If it doesn't, don't do anything.
                {
                    string[] getBio = message.Content.Split(' '); // Create array of all the stuff in Message.Content - Seperated by spaces.
                    getBio[0] = ""; getBio[1] = ""; getBio[2] = "";
                    if (string.Join(" ", getBio).Length > 200) { var del = await message.Channel.SendMessageAsync("The lobby bio can only be 200 characters max."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    finalOutput.Bio = string.Join(" ", getBio).Trim();
                }
            }
            finalOutput.Access = access;
            // Create the permissions variables, category, and channels -- add permissions to all.
            OverwritePermissions denyall = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny); OverwritePermissions inall = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit); OverwritePermissions fairuser = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny);

            var newcat = await guild.CreateCategoryChannelAsync($"{@message.Author.Username}'s Lobby LB");
            await newcat.AddPermissionOverwriteAsync(guild.EveryoneRole, denyall); await newcat.AddPermissionOverwriteAsync((message.Author as IUser), fairuser);
            var newvoicechannel = await guild.CreateVoiceChannelAsync($"{game} Voice Channel LB");
            await newvoicechannel.AddPermissionOverwriteAsync(guild.EveryoneRole, denyall); await newvoicechannel.AddPermissionOverwriteAsync((message.Author as IUser), fairuser);
            await (newvoicechannel as IVoiceChannel)?.ModifyAsync(x => { x.CategoryId = newcat.Id; });
            var newtextchannel = await guild.CreateTextChannelAsync($"{game} Text Channel LB");
            await newtextchannel.AddPermissionOverwriteAsync(guild.EveryoneRole, denyall); await newtextchannel.AddPermissionOverwriteAsync((message.Author as IUser), fairuser);
            await (newtextchannel as ITextChannel)?.ModifyAsync(x => { x.CategoryId = newcat.Id; });
            var keychannel = await guild.CreateTextChannelAsync($"key-channel-lb");
            await keychannel.AddPermissionOverwriteAsync(guild.EveryoneRole, denyall);
            await (keychannel as ITextChannel)?.ModifyAsync(x => { x.CategoryId = newcat.Id; });
            // Apply permissions to each channel, excluding main user.


            finalOutput.CategoryId = newcat.Id; // Add newcat to finalOutput

            List.Add(finalOutput);
            OwnsLobby.Add(message.Author.Id);
            var eb = new EmbedBuilder();
            if (access.ToLower() == "private")
            {
                eb.WithColor(Color.Blue).WithTitle("Welcome to your new lobby!").WithDescription("Because this lobby is private, you must create a key before users can join. " +
                    "Please do `lobbykey: key` (no spaces allowed, 12 char max.).\n\nFor additional commands, do `lobbyhelp:`");
            }
            else
            {
                eb.WithColor(Color.Blue).WithTitle("Welcome to your new lobby!").WithDescription("If you want to change it at all, just do `lobbyhelp:` to get a list of commands.");
            }
            await newtextchannel.SendMessageAsync($"{message.Author.Mention}", false, eb.Build());
            var ebmm = new EmbedBuilder();
            ebmm.WithColor(Color.Blue).WithTitle("How to use the GameLobby system:");
            ebmm.AddField("LobbyKey:", "Sets the key in a private lobby to something.\n12 characters maximum.\nTo use, do: `lobbykey: NewKeyHere`");

            ebmm.AddField("LobbyAccess:", "Change the access from public to private, or private to public.\nTo use, do `lobbyaccess: Public/Private`");

            ebmm.AddField("LobbyGame:", "Sets the game of the lobby.\nTo use, do: `lobbygame: NewGameHere`");

            ebmm.AddField("LobbyBio:", "Sets the lobby bio to something new. 200 characters maximum.\nTo use, do: `lobbybio: Write a short bio here.`");

            ebmm.AddField("DeleteLobby:", "Deletes the lobby.\nTo use, just do: `deletelobby:`");

            ebmm.AddField("LobbyInfo:", "Gets the current lobby information. Can only be used from within a lobby.\n" +
                "To use, just do: \n`lobbyinfo:` in a lobby.");
            await newtextchannel.SendMessageAsync("", false, ebmm.Build());
#pragma warning disable CS4014
            Browse(_client, newcat.Id, guild.Id);
            AutoDelete(_client, newtextchannel.Id, newvoicechannel.Id, newcat.Id);
#pragma warning restore CS4014
        }

        public static async Task BackendDeleteAllGuild(DiscordSocketClient _client, ulong GuildId)
        {
            var guild = _client.GetGuild(GuildId);
            int i = 0;
            foreach (var channel in guild.Channels)
            {
                if (channel.Name.ToLower().EndsWith("lb")) { await channel.DeleteAsync(); i++; }
            }
            foreach (var e in List)
            {
                try
                {
                    if (e.GuildId == GuildId)
                    {
                        OwnsLobby.Remove(e.OwnerId);
                        List.Remove(e);
                        if (List.Count() == 0) { break; }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public static async Task MessageDeleteAllGuild(DiscordSocketClient _client, SocketMessage message, bool SendMessage = true, bool DeleteMessage = true) // Deletes all lobbies within the message guild.
        {
            if (DeleteMessage) await message.DeleteAsync();
            var guild = (message.Author as SocketGuildUser).Guild;
            int i = 0;
            foreach (var channel in guild.Channels)
            {
                if (channel.Name.ToLower().EndsWith("lb")) { await channel.DeleteAsync(); i++; }
            }
            foreach (var e in List)
            {
                if (e.GuildId == guild.Id)
                {
                    OwnsLobby.Remove(e.OwnerId);
                    List.Remove(e);
                }
            }
            if (SendMessage == true) await message.Channel.SendMessageAsync($"Deleted {i / 3} lobbies.");
        }

        public static async Task ChangeKey(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true) // Changes the key of a lobby {List[index][6]} to any 12 character key.
        {
            if (DeleteMessage) await message.DeleteAsync();
            if (!message.Channel.Name.ToLower().EndsWith("lb")) { var del = await message.Channel.SendMessageAsync($"This command is only usable within a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            ulong channel = (message.Channel as SocketTextChannel).CategoryId.Value;
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == channel)
                {
                    if (!(lobby.OwnerId == message.Author.Id)) { var del = await message.Channel.SendMessageAsync("You must be the lobby owner to use this command."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    int index = List.FindIndex(prm => prm.Equals(lobby));
                    if (List[index].Access.ToLower() == "public") { var del = await message.Channel.SendMessageAsync("The lobby is currently public, please do `lobbyaccess: Private` to change the access to private."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    if (message.Content.Split(' ')[1].Length >= 12) { var del = await message.Channel.SendMessageAsync("Keys are a maximum of 12 characters. Please input a valid key."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    var end = lobby;
                    end.Key = message.Content.Split(' ')[1];
                    List[index] = end;
                    break;
                }
            }
        }

        public static async Task LeaveLobby(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true)
        {
            if (DeleteMessage) await message.DeleteAsync();
            if (!message.Channel.Name.ToLower().EndsWith("lb")) { var del = await message.Channel.SendMessageAsync($"This command is only usable within a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            ulong channel = (message.Channel as SocketTextChannel).CategoryId.Value;
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == channel)
                {
                    if (message.Author.Id == lobby.OwnerId) { await MessageDeleteLobby(_client, message, false); return; }
                    var guild = (message.Author as SocketGuildUser).Guild;
                    var chnls = guild.Channels.Where(x => x.CategoryId == lobby.CategoryId);
                    foreach (var chn in chnls)
                    {
                        await chn.RemovePermissionOverwriteAsync(message.Author as IUser);
                    }
                    break;
                }
            }
        }

        public static async Task ChangeAccess(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true) // Changes the Access of a lobby (public/private) to the specified mode.
        {
            if (DeleteMessage) await message.DeleteAsync();
            if (!(message.Content.ToLower().Split(' ')[1] == "private" || message.Content.ToLower().Split(' ')[1] == "public")) { var del = await message.Channel.SendMessageAsync($"Lobbies can only be public or private. Please use the correct syntax."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            if (!message.Channel.Name.ToLower().EndsWith("lb")) { var del = await message.Channel.SendMessageAsync($"This command is only usable within a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            ulong channel = (message.Channel as SocketTextChannel).CategoryId.Value;
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == channel)
                {
                    var end = lobby;
                    if (!(lobby.OwnerId == message.Author.Id))
                    {
                        var del = await message.Channel.SendMessageAsync("You must be the lobby owner to use this command."); await Task.Delay(5000); await del.DeleteAsync(); return;
                    }
                    int index = List.FindIndex(prm => prm.Equals(lobby));
                    if (List[index].Access.ToLower() == message.Content.Split(' ')[1].ToLower())
                    {
                        var del = await message.Channel.SendMessageAsync($"The lobby is already {lobby.Access}."); await Task.Delay(5000); await del.DeleteAsync(); return;
                    }
                    if (message.Content.Split(' ').Length == 1)
                    {
                        var del = await message.Channel.SendMessageAsync($"Please put if you want your lobby to be public or private."); await Task.Delay(5000); await del.DeleteAsync(); return;
                    }
                    else
                        end.Access = message.Content.Split(' ')[1];
                    List[index] = end;
                    if (List[index].Access.ToLower() == "private") await message.Channel.SendMessageAsync("Because this lobby is private, you must create a key before users can join. " +
                    "Please do `lobbykey: key` (no spaces allowed, 12 char max.).\n\nFor additional commands, do `lobbyhelp:`");
                    break;
                }
            }
        }

        public static async Task ChangeGame(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true) // Changes game (name) of a lobby. 80 Character max.
        {
            if (DeleteMessage) await message.DeleteAsync();
            ulong channel = (message.Channel as SocketTextChannel).CategoryId.Value;
            if (message.Content.Split(' ').Count() > 2) { var del = await message.Channel.SendMessageAsync($"A lobby name may not have spaces."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            if (message.Content.Split(' ')[1].Length >= 80) { var del = await message.Channel.SendMessageAsync($"Game name is 80 characters max. \nWhat game even has such a long name?"); await Task.Delay(5000); await del.DeleteAsync(); return; }
            if (!message.Channel.Name.ToLower().EndsWith("lb")) { var del = await message.Channel.SendMessageAsync($"This command is only usable within a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == channel)
                {
                    var end = lobby;
                    if (!(lobby.OwnerId == message.Author.Id)) { var del = await message.Channel.SendMessageAsync("You must be the lobby owner to use this command."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    var cc = (message.Author as SocketGuildUser).Guild.Channels.Where(x => x.CategoryId == lobby.CategoryId);
                    var ct = _client.GetChannel(message.Channel.Id);
                    var cv = _client.GetChannel(message.Channel.Id);
                    if (message.Content.Split(' ')[1].Contains("text") || message.Content.Split(' ')[1].Contains("voice")) { var del = await message.Channel.SendMessageAsync("Sorry, but games cannot contain the phrase Text or Voice."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    foreach (var cnl in cc)
                    {
                        if (cnl.Name.ToLower().Contains("text")) { ct = cnl; }
                        else if (cnl.Name.ToLower().Contains("voice")) { cv = cnl; }
                    }
                    int index = List.FindIndex(prm => lobby.Equals(prm));
                    end.Game = message.Content.Split(' ')[1];
                    try
                    {
                        await (ct as SocketTextChannel).ModifyAsync(x => { x.Name = message.Content.Split(' ')[1] + " Voice Channel LB"; });
                        await (cv as SocketVoiceChannel).ModifyAsync(x => { x.Name = message.Content.Split(' ')[1] + " Text Channel LB"; });
                    }
                    catch (Exception)
                    {
                        var del = await message.Channel.SendMessageAsync("Discord places a rate limit on changing channel names. \n\nPlease wait awhile to use this command again."); await Task.Delay(5000); await del.DeleteAsync(); return;
                    }
                    List[index] = end;
                    break;
                }
            }

        }

        public static async Task ChangeBio(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true) // Changes the bio of a lobby. 200 char max.
        {
            if (DeleteMessage) await message.DeleteAsync();
            if (message.Content.Split(' ').Length == 1) { var del = await message.Channel.SendMessageAsync("Please enter a lobby bio."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            var mray = message.Content.ToLower().Split(' ');
            ulong channel = (message.Channel as SocketTextChannel).CategoryId.Value;
            if (!message.Channel.Name.ToLower().EndsWith("lb")) { var del = await message.Channel.SendMessageAsync($"This command is only usable within a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == channel)
                {
                    if (!(lobby.OwnerId == message.Author.Id)) { var del = await message.Channel.SendMessageAsync("You must be the lobby owner to use this command."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    int index = List.FindIndex(prm => lobby.Equals(prm));
                    mray[0] = "";
                    var mr = string.Join(" ", mray);
                    if (mr.Length >= 200) { var del = await message.Channel.SendMessageAsync("The lobby bio can only be 200 characters max."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    var end = lobby;
                    end.Bio = mr.Trim();
                    List[index] = end;
                    break;
                }
            }

        }

        public static async Task MessageDeleteLobby(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true) // Deletes the lobby, channels, all lobby info, etc using a SocketMessage caller.
        {
            if (DeleteMessage) await message.DeleteAsync();
            ulong channel = (message.Channel as SocketTextChannel).CategoryId.Value;
            if (!message.Channel.Name.ToLower().EndsWith("lb")) { var del = await message.Channel.SendMessageAsync($"This command is only usable within a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == channel)
                {
                    if (!(lobby.OwnerId == message.Author.Id)) { var del = await message.Channel.SendMessageAsync("You must be the lobby owner to use this command."); await Task.Delay(5000); await del.DeleteAsync(); return; }
                    //Delete Channels
                    var chnls = (message.Author as SocketGuildUser).Guild.Channels.Where(x => x.CategoryId == lobby.CategoryId);
                    foreach (var chn in chnls)
                    {
                        await chn.DeleteAsync();
                    }
                    await (_client.GetChannel(lobby.CategoryId) as SocketCategoryChannel).DeleteAsync();
                    List.Remove(lobby);
                    OwnsLobby.Remove(message.Author.Id);
                    break;
                }
            }
        }

        public static async Task BackendDeleteLobby(DiscordSocketClient _client, ulong category)
        {
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == category)
                {
                    //Delete Channels
                    var chnls = _client.GetGuild(lobby.GuildId).Channels.Where(x => x.CategoryId == lobby.CategoryId);
                    foreach (var chn in chnls)
                    {
                        await chn.DeleteAsync();
                    }
                    await (_client.GetChannel(lobby.CategoryId) as SocketCategoryChannel).DeleteAsync();
                    OwnsLobby.Remove(lobby.OwnerId);
                    List.Remove(lobby);
                    break;
                }
            }
        } // Deletes a lobby, channels, all lobby info, etc using a GameLobby.BackendDelete(_client, CategoryId) call.

        public static async Task Info(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true) // Gets the lobby info and returns as an embed.
        {
            // Uses {OwnerId[2], Game Name[3], Bio[4], Access[5], Key[6]}
            if (DeleteMessage) await message.DeleteAsync();
            ulong channel = (message.Channel as SocketTextChannel).CategoryId.Value;
            if (!message.Channel.Name.ToLower().EndsWith("lb")) { var del = await message.Channel.SendMessageAsync($"This command is only usable within a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return; }
            foreach (var lobby in List)
            {
                if (lobby.CategoryId == channel)
                {
                    var eb = new EmbedBuilder();
                    eb.WithColor(Color.Blue).WithTitle("Your lobby info:").AddField("Owner:", _client.GetUser(lobby.OwnerId).Mention).AddField("Game:", lobby.Game);
                    if (lobby.Bio != null)
                    {
                        eb.AddField("Bio:", lobby.Bio);
                    }
                    eb.AddField("Access:", lobby.Access);
                    if (lobby.Access.ToLower() == "private")
                    {
                        if (lobby.Key == null) eb.AddField("Key:", "You don't actually have a key set. Please do `lobbykey:` to add a key.");
                        else
                            eb.AddField("Key:", lobby.Key);
                    }
                    await message.Channel.SendMessageAsync("", false, eb.Build());
                }
            }
        }

        public static async Task Browse(DiscordSocketClient _client, ulong category, ulong serverid) // Browsing - do every time a new channel is made. 
        {
            ulong mchannel = _client.GetGuild(serverid).Channels.Where(x => x.Name.ToLower() == "gllobbies").First().Id;
            ListItems currentlobby = List.Where(x => x.CategoryId == category).First();
            #region make embed
            var se = new EmbedBuilder();
            se.WithColor(Color.Blue).WithTitle("Lobby Information:").AddField("Owner:", _client.GetUser(currentlobby.OwnerId).Mention).AddField("Game:", currentlobby.Game);
            if (currentlobby.Bio != null)
            {
                se.AddField("Bio:", currentlobby.Bio);
            }
            se.AddField("Access:", currentlobby.Access).WithFooter("To join this lobby, click the green reaction below. It may take a few seconds, so be paitent.");
            #endregion
            var message = await (_client.GetChannel(mchannel) as SocketTextChannel).SendMessageAsync("", false, se.Build());
            var aa = new EmbedBuilder();

            var emote = _client.GetGuild(406657890921742336).Emotes.First() as IEmote;
            await message.AddReactionAsync(emote);
            List<ulong> joined = new List<ulong>
            {
                _client.CurrentUser.Id,
                currentlobby.OwnerId
            };
            var guild = _client.GetGuild(currentlobby.GuildId);
            // Create permissions
            OverwritePermissions denyall = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny); OverwritePermissions inall = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit); OverwritePermissions fairuser = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny);

            await Task.Delay(5000);
            while (true) // Goes until break;
            {
                // Updates browser
                try
                {
                    var exists = List.Where(x => x.CategoryId == category);
                    if (exists.Count() == 0) // Does not exist anymore.
                    {
                        await message.DeleteAsync();
                        break;
                    }
                    else // Still exists. 
                    {
                        // Set currentlobby to be the actual current lobby.
                        currentlobby = exists.First();

                        // Create correct embed
                        var eb = new EmbedBuilder();
                        eb.WithColor(Color.Blue).WithTitle("Lobby Information:").AddField("Owner:", _client.GetUser(currentlobby.OwnerId).Mention).AddField("Game:", currentlobby.Game);
                        if (currentlobby.Bio != null)
                        {
                            eb.AddField("Bio:", currentlobby.Bio);
                        }
                        eb.AddField("Access:", currentlobby.Access).WithFooter("To join this lobby, click the green reaction below. It may take a few seconds, so be paitent.");

                        if (currentlobby.Bio != null)
                        {
                            if (eb.Build().Fields[1].Value == message.Embeds.First().Fields[1].Value && eb.Build().Fields[2].Value == message.Embeds.First().Fields[2].Value
                                && eb.Build().Fields[3].Value == message.Embeds.First().Fields[3].Value)
                            {
                                // do nothing 
                            }
                            else
                            {
                                await message.ModifyAsync(x => { x.Embed = eb.Build(); });
                            }
                        }
                        else
                        {
                            if (eb.Build().Fields[1].Value == message.Embeds.First().Fields[1].Value && eb.Build().Fields[2].Value == message.Embeds.First().Fields[2].Value)
                            {
                                // do nothing
                            }
                            else
                            {
                                await message.ModifyAsync(x => { x.Embed = eb.Build(); });
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }



                // Checks for new users
                try
                {
                    var users = await message.GetReactionUsersAsync(emote);
                    List<IUser> newu = new List<IUser>();
                    foreach (var user in users)
                    {
                        if (joined.Contains(user.Id)) { }
                        else newu.Add(user);
                        if (user.Id != _client.CurrentUser.Id)
                        {
                            await Task.Delay(1000); // Try to not ratelimit.
                            await message.RemoveReactionAsync(emote, user); // Removes reaction
                        }
                    }
                    foreach (var user in newu) // Does something for every new user that has joined.
                    {
                        joined.Add(user.Id); // Adds to list so it counts as new user.
                        // Join Lobby
                        var chnls = guild.Channels.Where(x => x.CategoryId == category);
                        foreach (var chnl in chnls) // Apply new permissions and such.
                        {
                            if (currentlobby.Access.ToLower() == "private")
                            {
                                var keyc = chnls.Where(chn => !(chn.Name.ToLower().Contains("text") || chn.Name.ToLower().Contains("voice"))).First();
                                await keyc.AddPermissionOverwriteAsync(user, fairuser);
                                await (keyc as SocketTextChannel).SendMessageAsync(user.Mention + " please input the lobby key.\n\nDo this by doing `joinkey: {key}`\n\n(If you do not know the key, you can do `leavelobby:` to exit.)");
                                break;
                            }
                            else
                            {
                                await (_client.GetChannel(currentlobby.CategoryId) as SocketCategoryChannel).AddPermissionOverwriteAsync(user, fairuser);
                                await (chnls.Where(x => x.Name.ToLower().Contains("text")).First() as SocketTextChannel).SendMessageAsync($"{user.Mention} has joined the lobby! You can do `leavelobby:` at any time to leave it.");
                                break;
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
                await Task.Delay(5000); // Just so it slows down a bit - not as frantic checking. Saves my PC.  
            }
        }

        //public static async Task JoinLobby(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true)
        //{
        //    // PLEASE NOTE: This is kept here for no real reason. The system is no longer in use.
        //    if (DeleteMessage) await message.DeleteAsync();
        //    if (message.Content.Split(' ').Length > 2)
        //    {
        //        var del = await message.Channel.SendMessageAsync($"Please use the correct syntax. Do `lobbyhelp:` if you need help."); await Task.Delay(5000); await del.DeleteAsync(); return;
        //    }
        //    if (message.MentionedUsers.Count == 0)
        //    {
        //        var del = await message.Channel.SendMessageAsync($"Hey! Please only use @ mentions for this command."); await Task.Delay(5000); await del.DeleteAsync(); return;
        //    }
        //    var oid = message.MentionedUsers.First().Id;
        //    ListItems lobby;
        //    try
        //    {
        //        lobby = List.Where(x => x.OwnerId == oid).First();
        //    }
        //    catch (Exception)
        //    {
        //        var del = await message.Channel.SendMessageAsync($"Sorry, that user does not seem to own a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return;
        //    }
        //    if (lobby.GuildId != (message.Author as SocketGuildUser).Guild.Id)
        //    {
        //        var del = await message.Channel.SendMessageAsync($"Sorry, that user does not seem to own a lobby in this server."); await Task.Delay(5000); await del.DeleteAsync(); return;
        //    }
        //    if (message.Author.Id == lobby.OwnerId)
        //    {
        //        var del = await message.Channel.SendMessageAsync($"You cannot join your own lobby."); await Task.Delay(5000); await del.DeleteAsync(); return;
        //    }
        //    var chnls = (message.Author as SocketGuildUser).Guild.Channels.Where(x => x.CategoryId == lobby.CategoryId);
        //    // Create permissions
        //    OverwritePermissions denyall = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny); OverwritePermissions inall = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit); OverwritePermissions fairuser = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny);

        //    if (lobby.Access.ToLower() == "private")
        //    {
        //        var keyc = chnls.Where(chn => !(chn.Name.ToLower().Contains("text") || chn.Name.ToLower().Contains("voice"))).First();
        //        await keyc.AddPermissionOverwriteAsync((message.Author as IUser), fairuser);
        //        await (keyc as SocketTextChannel).SendMessageAsync(message.Author.Mention + " please input the lobby key.\n\nDo this by doing `joinkey: {key}`");
        //        return;
        //    }
        //    else
        //    {
        //        await (_client.GetChannel(lobby.CategoryId) as SocketCategoryChannel).AddPermissionOverwriteAsync((message.Author as IUser), fairuser);
        //        await (chnls.Where(x => x.Name.ToLower().Contains("text")).First() as SocketTextChannel).SendMessageAsync($"{message.Author.Username} has joined the lobby!");
        //        return;
        //    }
        //} // User joining lobby. Starting command

        public static async Task JoinPrivate(DiscordSocketClient _client, SocketMessage message)
        {
            await message.DeleteAsync();
            // Make sure key is correct
            if (message.Content.Split(' ').Length > 2)
            {
                var del = await message.Channel.SendMessageAsync($"Please use the correct syntax. Do `lobbyhelp:` if you need help."); await Task.Delay(5000); await del.DeleteAsync(); return;
            }
            ListItems lobby;
            try
            {
                lobby = List.Where(x => x.CategoryId == (message.Channel as SocketTextChannel).CategoryId).First();
            }
            catch (Exception)
            {
                var del = await message.Channel.SendMessageAsync($"Sorry, that user does not seem to own a lobby."); await Task.Delay(5000); await del.DeleteAsync(); return;
            }
            if (lobby.GuildId != (message.Author as SocketGuildUser).Guild.Id)
            {
                var del = await message.Channel.SendMessageAsync($"Sorry, that user does not seem to own a lobby in this server."); await Task.Delay(5000); await del.DeleteAsync(); return;
            }
            if (message.Content.Split(' ')[1] != lobby.Key)
            {
                var del = await message.Channel.SendMessageAsync($"It appears that you have the incorrect key. Please try again."); await Task.Delay(5000); await del.DeleteAsync(); return;
            }

            // Give permissions. Take key permissions.
            var chnls = (message.Author as SocketGuildUser).Guild.Channels.Where(x => x.CategoryId == (message.Channel as SocketTextChannel).CategoryId);
            OverwritePermissions denyall = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny); OverwritePermissions inall = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit); OverwritePermissions fairuser = new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny);
            await (_client.GetChannel(Convert.ToUInt64((message.Channel as SocketTextChannel).CategoryId)) as SocketCategoryChannel).AddPermissionOverwriteAsync((message.Author as IUser), fairuser);
            foreach (var chn in chnls)
            {
                if (!(chn.Name.ToLower().Contains("text") || chn.Name.ToLower().Contains("voice")))
                    await chn.AddPermissionOverwriteAsync((message.Author as IUser), denyall);
            }
            await (chnls.Where(x => x.Name.ToLower().Contains("text")).First() as SocketTextChannel).SendMessageAsync($"{message.Author.Mention} has joined the lobby!");
            return;

        } // Called when somebody types in the key channel. (joinkey:)

        public static async Task AutoDelete(DiscordSocketClient _client, ulong Utext, ulong Uvoip, ulong Ucat)
        {
            var text = _client.GetChannel(Utext) as SocketTextChannel;
            var voip = _client.GetChannel(Uvoip) as SocketVoiceChannel;
            var mincount = 0;
            ulong lastmessageid = 0;
            while (true)
            {
                mincount++;
                if (voip.Users.Count > 0) { mincount = 0; }
                var killme = await text.GetMessagesAsync(1).FlattenAsync();
                if (killme.First().Id != lastmessageid)
                {
                    lastmessageid = killme.First().Id;
                    mincount = 0;
                }
                if (mincount >= 10) // Final thing - delete lobby. Int after >= statement is total number of minutes of inactivity it takes to delete.
                {
                    await BackendDeleteLobby(_client, Ucat);
                    break;
                }
                await Task.Delay(1000 * 60); // Waits a minute to check
            }
        }
        #endregion



        public static async Task LobbyHelp(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true) // Sends a help message. Changes depending if the channel is a lobby or not.
        {
            if (DeleteMessage) await message.DeleteAsync();
            var eb = new EmbedBuilder();
            eb.WithColor(Color.Blue).WithTitle("How to use the GameLobby system:");
            if (message.Channel.Name.ToLower().EndsWith("lb")) // Is in a lobby
            {
                eb.AddField("LobbyKey:", "Sets the key in a private lobby to something.\n12 characters maximum.\nTo use, do: `lobbykey: NewKeyHere`");

                eb.AddField("LobbyAccess:", "Change the access from public to private, or private to public.\nTo use, do `lobbyaccess: Public/Private`");

                eb.AddField("LobbyGame:", "Sets the game of the lobby.\nTo use, do: `lobbygame: NewGameHere`");

                eb.AddField("LobbyBio:", "Sets the lobby bio to something new. 200 characters maximum.\nTo use, do: `lobbybio: Write a short bio here.`");

                eb.AddField("DeleteLobby:", "Deletes the lobby.\nTo use, just do: `deletelobby:`");

                eb.AddField("LobbyInfo:", "Gets the current lobby information. Can only be used from within a lobby.\n" +
                    "To use, just do: \n`lobbyinfo:` in a lobby.");
                eb.WithFooter("Hey! Want to invite me to your own server? Do invite: to get an invite link DM'd to you!");

            }
            else // Isn't in a lobby.
            {
                eb.AddField("NewLobby:", "Creates a new lobby. The optional lobby bio cannot be more than 200 characters long. " +
                    "A public lobby can be joined by anyone, and a private lobby can only be joined by somebody with a key. If no privacy modifier is given, it defaults to public. " +
                    "You can set the key from within the lobby.\n" +
                    "To use the command, do: ```newlobby: GameName Public/Private A short desciption of the lobby!```");
                eb.AddField("Examples:", "```newlobby: Overwatch``````newlobby: Overwatch Public``````newlobby: Overwatch Private Quick Play```");
                eb.WithFooter("You can get even more commands by doing the `lobbyhelp:` command in a lobby channel! Want to invite me to your own server? Do invite: to get an invite link DM'd to you!");
            }
            if ((message.Author as SocketGuildUser).Roles.Where(x => x.Permissions.Administrator == true).Count() >= 1)
            {
                eb.AddField("Extra admin commands:","\u200B");
                eb.AddField("ForceDelete:", "Deletes a specific lobby. You must use it from within the text channel inside of the desired lobby.\nTo use, just do: `ForceDelete:`");
                eb.AddField("DeleteAll:", "Deletes every lobby in your guild. \nTo use, just do: `DeleteAll:` in any channel.");
            }
            await message.Channel.SendMessageAsync("", false, eb.Build());
        }
    }
}