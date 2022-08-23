using Discord.WebSocket;

namespace Proxmox.Permission{
    public class Checker{
        public bool isAdmin(SocketGuildUser user){
            return user.GuildPermissions.Administrator;
        }
    }
}