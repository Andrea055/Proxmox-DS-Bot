using System;
using System.IO;
using Corsinvest.ProxmoxVE.Api;
using Discord.WebSocket;

namespace Proxmox.Bot.Handler{
    public class RAM{
        public async void Handler(SocketMessageComponent component, string inforaw){
            var info = inforaw.Split("/");
            var id = info[1];
            var username = info[2];
            var password = info[3];
            var ip = info[4];
            var node = info[5];
            var client = new PveClient(ip);
            if (await client.Login(username, password)){
                client.ResponseType = ResponseType.Png;
                var graph_raw = client.Nodes[node].Qemu[id].Rrd.Rrd("total","week").Result;
                var graph = graph_raw.Response;
                var bytes = Convert.FromBase64String(graph.Replace("data:image/png;base64,", ""));
                var stream = new MemoryStream(bytes);
                await component.RespondWithFileAsync(fileStream: stream, fileName: "memory" + ".png");
            }
        }
    }
}