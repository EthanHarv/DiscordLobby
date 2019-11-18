using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace DiscordLobby
{
    class UserChannels
    {
        /*User Channels are channels made by server admins that are natively hidden from view, but a permission overwrite system allows users to opt-in to seeing it.
         This process requires both Newtonsoft.Json and Discord.Net to properly work. Discord.Net, of course, is used to interface with the discord API, and the JSON
         is going to be used to store things such as the Channel Id, Bio, etc that needs to be displayed for users to join. Users join through an interface similar to the
         one used in the GameLobby system. Channels are, however, assignable.*/

        // This function pretty much just says "ay biggums go do stuff."
        public static async Task CreateChannelFromMessage(DiscordSocketClient _client, SocketMessage message, bool DeleteMessage = true)
        {
            if (DeleteMessage) await message.DeleteAsync();
            //???? What did I use this for again?
        }

    }
}
