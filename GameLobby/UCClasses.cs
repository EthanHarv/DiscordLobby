using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;


namespace DiscordLobby
{
    class UCJoinPortal
    {
        public ulong ChannelId { get; set; }
        public ulong ServerId { get; set; }
        public List<UCChannel> Channels { get; set; } = new List<UCChannel>();
    }
    class UCChannel
    {
        public ulong ChannelId { get; set; }
        public string Bio { get; set; }
        public List<UCPermission> Permissions { get; set; } = new List<UCPermission>();
        public UCPermission DefaultPermissions { get; set; }
    }
    class UCPermission
    {
        public ulong UserId { get; set; }
        public int AllowValue { get; set; }
        public int DenyValue { get; set; }
    }
}
