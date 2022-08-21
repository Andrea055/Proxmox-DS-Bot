using Discord;
using Discord.WebSocket;

namespace Proxmox.Bot.Handler{
    public class Graph{
        public async void Handler(SocketMessageComponent component, string info){
            var raw_data = info.Split("/");
            var id = raw_data[0].Replace("graphID:", "");
            var username = raw_data[1].Replace("User:", "");
            var password = raw_data[2].Replace("Password:", "");
            var ip = raw_data[3].Replace("IP:", "");
            var node = raw_data[4].Replace("Node:", "");
            var select = new ComponentBuilder()
                .WithButton("Cpu", $"cpu/{id}/{username}/{password}/{ip}/{node}", ButtonStyle.Success)
                .WithButton("Ram", $"ram/{id}/{username}/{password}/{ip}/{node}", ButtonStyle.Success)
                .WithButton("Network", $"network/{id}/{username}/{password}/{ip}/{node}", ButtonStyle.Success)
                .WithButton("Disk I/O", $"disk/{id}/{username}/{password}/{ip}/{node}", ButtonStyle.Success);
            await component.RespondAsync(components: select.Build());
        }
    }
}