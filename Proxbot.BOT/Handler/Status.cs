using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using Corsinvest.ProxmoxVE.Api;
using System.Collections.Generic;

namespace Proxmox.Bot.Handler{
    public class Status{
        async public void Handler(SocketMessageComponent component,string info ){
            var raw_data = info.Split("/");
            var id = raw_data[0].Replace("statusID:", "");
            var username = raw_data[1].Replace("User:", "");
            var password = raw_data[2].Replace("Password:", "");
            var ip = raw_data[3].Replace("IP:", "");
            var node = raw_data[4].Replace("Node:", "");
            var client = new PveClient(ip);
            if (await client.Login(username, password))
            {
                Console.WriteLine(node);
                Console.WriteLine(id);
                var status_raw = await client.Nodes[node].Qemu[id].Status.Current.VmStatus();
                var status = status_raw.Response.data;
                var id_emb = new EmbedFieldBuilder()
                    .WithName("ID:")
                    .WithValue($"{status.vmid}");
                var Status = new EmbedFieldBuilder()
                    .WithName("Status")
                    .WithValue(status.status);
                var cpu = new EmbedFieldBuilder() 
                    .WithName("Cpus")
                    .WithValue(status.cpus)
                    .WithValue($"Cpu usage: {status.cpu}");
                var ram = new EmbedFieldBuilder()
                    .WithName("RAM")
                    .WithValue(status.maxmem);
                var net = new EmbedFieldBuilder()
                    .WithName("Network")
                    .WithValue($"NetOut: {status.netout}")
                    .WithValue($"NetIn: {status.netin}");
                var disk_emb = new EmbedFieldBuilder()
                    .WithName("Disk")
                    .WithValue($"Disk Read: {status.diskread}");
                   
               
                var embed = new EmbedBuilder
                    {
                    Title = status.name,
                    Description = status.status,  
                    Fields = new List<EmbedFieldBuilder>{
                        id_emb, Status, cpu,ram, net, disk_emb
                    }        
                    };
                    await component.RespondAsync("",  embed: embed.Build());
            }
        }
    }
}