using System;
using System.IO;
using System.Collections.Generic;
using Corsinvest.ProxmoxVE.Api;
using Discord.Interactions;
using Discord.WebSocket;
using Proxmox.BOT.Image;

namespace Proxmox.Bot.Handler{
    public class IO : InteractionModuleBase<SocketInteractionContext>{
     
        public async void Handler(SocketMessageComponent component, string inforaw){
            var info = inforaw.Split("/");
            var type = info[0];
            var id = info[1];
            var username = info[2];
            var password = info[3];
            var ip = info[4];
            var node = info[5];
            var client = new PveClient(ip);
            if (await client.Login(username, password)){
                client.ResponseType = ResponseType.Png;
                var graph_raw = client.Nodes[node].Qemu[id].Rrd.Rrd("diskread","week").Result;
                var graph = graph_raw.Response;
                var bytes = Convert.FromBase64String(graph.Replace("data:image/png;base64,", ""));
                var diskread = new MemoryStream(bytes);
                graph_raw = client.Nodes[node].Qemu[id].Rrd.Rrd("diskwrite","week").Result;
                graph = graph_raw.Response;
                bytes = Convert.FromBase64String(graph.Replace("data:image/png;base64,", ""));
                var diskout = new MemoryStream(bytes);
                var images = new List<Stream>{
                    diskread, diskout
                };
                var merger = new ImageManipolation();
                var allgraph = merger.Merge(images);
                var graph_image_raw = allgraph.Encode();
                await component.RespondWithFileAsync(fileStream: graph_image_raw.AsStream(), fileName: "diskread" + ".png");
            }
        }
    }
}