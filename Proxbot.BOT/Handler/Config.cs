using Corsinvest.ProxmoxVE.Api;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;

namespace Proxmox.Bot.Handler{
    public class Config{
        async public void Handler(SocketMessageComponent component, string info ){
            var raw_data = info.Split("/");
            var id = raw_data[0].Replace("configID:", "");
            var username = raw_data[1].Replace("User:", "");
            var password = raw_data[2].Replace("Password:", "");
            var ip = raw_data[3].Replace("IP:", "");
            var node = raw_data[4].Replace("Node:", "");
            var client = new PveClient(ip);
            if(await client.Login(username, password)){
                var config_raw = await client.Nodes[node].Qemu[id].Config.VmConfig();
                var config = config_raw.Response.data;
                var cpu_type = new EmbedFieldBuilder()
                    .WithName("CPU Type:")
                    .WithValue($"{config.cpu}");
                var cores = new EmbedFieldBuilder()
                    .WithName("Cores:")
                    .WithValue($"{config.cores}");
                var sockets = new EmbedFieldBuilder()
                    .WithName("Sockets:")
                    .WithValue($"{config.sockets}");
                var memory = new EmbedFieldBuilder()
                    .WithName("Memory:")
                    .WithValue($"{config.memory}");
                var boot_order = new EmbedFieldBuilder()
                    .WithName("Boot order:")
                    .WithValue($"{config.boot}");
                EmbedFieldBuilder on_boot;
                try{
                    on_boot = new EmbedFieldBuilder()
                        .WithName("Start on boot:")
                        .WithValue($"{config.onboot}");
                }catch{
                    on_boot = new EmbedFieldBuilder()
                        .WithName("Start on boot:")
                        .WithValue($"0");
                }
           
                    var embed = new EmbedBuilder
                    {
                    Title = config.name,
                    Fields = new List<EmbedFieldBuilder>{
                        cpu_type, cores, sockets, memory, boot_order, on_boot
                    }        
                    };
                    await component.RespondAsync("",  embed: embed.Build());
            }
        }
    }
}