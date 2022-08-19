using Discord.WebSocket;
using Corsinvest.ProxmoxVE.Api;
namespace Proxmox.Bot.Handler{
    public class Reset{
        async public void Handler(SocketMessageComponent component, string info ){
            var user = component.User as SocketGuildUser;
            if(user.GuildPermissions.Administrator){
                var raw_data = info.Split("/");
                var id = raw_data[0].Replace("resetID:", "");
                var username = raw_data[1].Replace("User:", "");
                var password = raw_data[2].Replace("Password:", "");
                var ip = raw_data[3].Replace("IP:", "");
                var node = raw_data[4].Replace("Node:", "");
                var client = new PveClient(ip);
                if (await client.Login(username, password))
                {
                    var stop_raw = await client.Nodes[node].Qemu[id].Status.Reset.VmReset();
                    await component.RespondAsync("VM reset successfully");
                }
            }else{
                await component.RespondAsync("You don't have the permission to do that, only Administrator can Start, Shutdown, Stop, Reboot and Reset the VM");
            };
            
        }
    }
}