using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;

namespace DiscordLobby // Wrote this code about a year/two years ago. (8th grade.) Haven't updated since. 8th grade me probably didn't use the greatest practices. Be warned.
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        public static bool Online = false;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;

            string token = File.ReadAllText(@"C:\Users\Zonee\source\repos\DiscordLobby\GameLobby\BOTTOKEN.btk"); // Bot token. Change to whatever path needed.
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await _client.SetGameAsync("lobbyhelp:"); // No longer works with current API afaik, but this is 2019 Ethan and I haven't checked.

            Directory.CreateDirectory(@"C:\DiscordLobby");

            #region startup and shutdown
            //Startup
            await Task.Delay(5000);
            foreach (var guild in _client.Guilds)
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Deleting lobbies in {guild}...");
                await GameLobby.BackendDeleteAllGuild(_client, guild.Id);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Lobbies deleted in {guild}.");
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Clearing lobby channel in {guild}.");
                try
                {
                    ulong ChannelId = guild.Channels.Where(x => x.Name.ToLower() == "gllobbies").First().Id;
                    var msgs = await (_client.GetChannel(ChannelId) as ISocketMessageChannel).GetMessagesAsync().FlattenAsync();
                    while (msgs.Count() > 0)
                    {
                        var save = msgs;
                        await (_client.GetChannel(ChannelId) as SocketTextChannel).DeleteMessagesAsync(msgs);
                        msgs = await (_client.GetChannel(ChannelId) as SocketTextChannel).GetMessagesAsync(save.Last().Id, Direction.Before, 99).FlattenAsync();
                    }
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Cleared lobby channel in {guild}.");
                }
                catch (Exception)
                {
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Error       {guild} did not have a channel with that name.");
                }
            }
            Online = true;

            //Shutdown
            while (true)
            {
                var b = Console.ReadKey();
                Console.WriteLine();
                if (b.KeyChar.ToString().ToLower() == "y")
                {
                    Online = false;
                    try
                    {
                        foreach (var guild in _client.Guilds)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Deleting lobbies in {guild}...");
                            await GameLobby.BackendDeleteAllGuild(_client, guild.Id); // This is where it messes up.
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Lobbies deleted in {guild}.");
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Clearing lobby channel in {guild}.");
                            try
                            {
                                ulong ChannelId = guild.Channels.Where(x => x.Name.ToLower() == "gllobbies").First().Id;
                                var msgs = await (_client.GetChannel(ChannelId) as ISocketMessageChannel).GetMessagesAsync().FlattenAsync();
                                while (msgs.Count() > 0)
                                {
                                    var save = msgs;
                                    await (_client.GetChannel(ChannelId) as SocketTextChannel).DeleteMessagesAsync(msgs);
                                    msgs = await (_client.GetChannel(ChannelId) as SocketTextChannel).GetMessagesAsync(save.Last().Id, Direction.Before, 99).FlattenAsync();
                                }
                                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Startup     Cleared lobby channel in {guild}.");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Error       {guild} did not have a channel with that name.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    await Task.Delay(500);
                    Console.WriteLine("Now ready to shut down. Please press any key to continue.");
                    Console.ReadKey();
                    break;
                }
            }

            #endregion
        }

        public async Task MessageReceived(SocketMessage message)
        {
            if (!Online) // Bot is offline if called
            {
                return;
            }
            try // Wraps in try-catch just in case if something breaks.
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                // Lobby System.
                #region GameLobby
                var guild = (message.Author as SocketGuildUser).Guild;
                #region admin commands
                if (message.Content.ToLower() == "setuplobbies:") // Create the gllobbies channel. Admin/Zonee can use it.
                {
                    try // Dunno something might go wrong. Might as well have this here lmao.
                    {
                        if (((message.Author as SocketGuildUser).Roles.Where(x => x.Permissions.Administrator == true).Count() >= 1 || message.Author.Id == 198110625065467905)) // Is admin or Zonee
                        {
                            CreateChannel(message);
                            return;
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync("Hey! Sorry, but you do not seem to have admin permissions. Please get an admin to use this command.");
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        await message.Channel.SendMessageAsync("Sorry, something went wrong."); // Isn't Admin/Zonee
                    }
                }
                if (message.Content.ToLower().StartsWith("forcedelete:") && (message.Author as SocketGuildUser).Roles.Where(x => x.Permissions.Administrator == true).Count() >= 1) // Deletes 1 lobby
                {
                    DeleteLobby(_client, message);
                    return;
                }
                if (message.Content.ToLower().StartsWith("deleteall:") && (message.Author as SocketGuildUser).Roles.Where(x => x.Permissions.Administrator == true).Count() >= 1) // Deletes all lobbies in a guild
                {
                    GameLobby.MessageDeleteAllGuild(_client, message);
                    return;
                }
                #endregion
                if (message.Content.ToLower().StartsWith("newlobby:"))
                {
                    if (guild.Channels.Where(x => x.Name.ToLower() == "gllobbies").Count() == 0 || guild.Channels.Where(x => x.Name.ToLower() == "gllobbies").Count() > 1)
                    {
                        await message.Channel.SendMessageAsync($"Hey! Your server doesn't seem to have the proper channel made yet, or it has multiple. Please have a server admin do `SetUpLobbies:`.");
                        return;
                    }
                    GameLobby.NewLobby(_client, message);
                    return;
                }
                if (message.Content.ToLower() == "invite:")
                {
                    await message.Author.SendMessageAsync("https://discordapp.com/oauth2/authorize?client_id=446798103866245120&scope=bot&permissions=8");
                    await message.Channel.SendMessageAsync(message.Author.Mention + "DM'd you a message, make sure that you have server DM's enabled!");
                    return;
                }
                if (message.Content.ToLower().StartsWith("lobbykey:"))
                {
                    GameLobby.ChangeKey(_client, message);
                    return;
                }
                if (message.Content.ToLower().StartsWith("lobbyaccess:"))
                {
                    GameLobby.ChangeAccess(_client, message);
                    return;
                }
                if (message.Content.ToLower().StartsWith("lobbygame:"))
                {
                    GameLobby.ChangeGame(_client, message);
                    return;
                }
                if (message.Content.ToLower().StartsWith("lobbybio:"))
                {
                    GameLobby.ChangeBio(_client, message);
                    return;
                }
                if (message.Content.ToLower().StartsWith("deletelobby:"))
                {
                    GameLobby.MessageDeleteLobby(_client, message);
                    return;
                }
                if (message.Content.ToLower().StartsWith("lobbyinfo:"))
                {
                    GameLobby.Info(_client, message);
                    return;
                }
                if (message.Content.ToLower().StartsWith("joinkey:"))
                {
                    GameLobby.JoinPrivate(_client, message);
                    return;
                }
                if (message.Content.ToLower() == "lobbyhelp:")
                {
                    GameLobby.LobbyHelp(_client, message);
                    return;
                }
                if (message.Content.ToLower() == "leavelobby:")
                {
                    GameLobby.LeaveLobby(_client, message);
                    return;
                }
                #endregion
                // Checks to make sure the message has the proper : attached to it. If it does not, send an error message to the user.
                #region MessageParseError (: checker)
                string[] commands = new string[] { "newlobby", "lobbykey", "lobbyaccess", "lobbygame", "lobbybio", "deletelobby", "lobbyinfo", "joinlobby", "joinkey", "lobbyhelp", "leavelobby" };
                if (commands.Contains(message.Content.ToLower().Split(' ')[0]))
                {
                    NoColonErrorMessage(message);
                }
                #endregion
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something broke in MessageRecieved\nIt was sent by {message.Author} in server {(message.Author as SocketGuildUser).Guild} at {DateTime.Now.ToLongDateString()}");
                Console.WriteLine(e);
            }            
        }

        public static async Task CreateChannel(SocketMessage message)
        {
            var guild = (message.Author as SocketGuildUser).Guild;
            if (guild.Channels.Where(x => x.Name.ToLower() == "gllobbies").Count() == 1)
            {
                await message.Channel.SendMessageAsync("It seems that the channel already exists.");
                return;
            }
            else if (guild.Channels.Where(x => x.Name.ToLower() == "gllobbies").Count() > 1)
            {
                await message.Channel.SendMessageAsync("It seems that you have multiple channels already made for this. Please delete all but one (or all, and then do this command again.)");
                return;
            }
            var chnl = await guild.CreateTextChannelAsync("gllobbies");
            OverwritePermissions permissions = new OverwritePermissions(65536, 2048);
            await chnl.AddPermissionOverwriteAsync(guild.EveryoneRole, permissions);
        }

        public static async Task DeleteLobby(DiscordSocketClient _client, SocketMessage message)
        {
            if (!message.Channel.Name.ToLower().EndsWith("lb"))
            {
                await message.Channel.SendMessageAsync("You can only use this command in a lobby.");
                return;
            }
            await GameLobby.BackendDeleteLobby(_client, (message.Channel as SocketTextChannel).CategoryId.Value);
            await message.Author.SendMessageAsync("Lobby deleted.");
        }

        public static async Task NoColonErrorMessage(SocketMessage message)
        {
            await message.DeleteAsync();
            var del = await message.Channel.SendMessageAsync($"Hey! Please remember to put the **:** in {message.Content.Split(' ')[0].ToLower()}:."); await Task.Delay(7500); await del.DeleteAsync(); return;
        }

        public static async Task TotalHelpMessage(SocketMessage message)
        {
            //You need to write this. Please dont forget to write this. You need to do that. Also come up with a caller name. Thats important. 
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
