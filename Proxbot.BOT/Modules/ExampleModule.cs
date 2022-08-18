using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using System.Collections.Generic;
using System;

namespace Proxmox.BOT
{
    // Interation modules must be public and inherit from an IInterationModuleBase
    public class ExampleModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private InteractionHandler _handler;

        // Constructor injection is also a valid way to access the dependencies
        public ExampleModule(InteractionHandler handler)
        {
            _handler = handler;
        }

        [SlashCommand("ping", "Pings the bot and returns its latency.")]
        public async Task GreetUserAsync()
            => await RespondAsync(text: $":ping_pong: It took me {Context.Client.Latency}ms to respond to you!", ephemeral: true);

        [SlashCommand("specs", "Gets Specification of server")]
        public async Task Specs([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            
            var client = new PveClient(ip);
            if (await client.Login(username, password))
            {
                        
                var vm = client.Nodes[node].Status;
                var specs = await vm.Status();
                var response = specs.Response.data;
                var cpu = new EmbedFieldBuilder()
                    .WithName($"CPU: {response.cpuinfo.sockets}x {response.cpuinfo.model}")
                    .WithValue(response.cpuinfo.cpus + " cores @" + response.cpuinfo.mhz + "mHz");
                var cpusage = new EmbedFieldBuilder()
                    .WithName("CPU Usage:")
                    .WithValue(response.cpu);
                var ram = new EmbedFieldBuilder()
                    .WithName($"Memory: ")
                    .WithValue($"Total: {response.memory.total} \n Used: {response.memory.used} \n Free: {response.memory.free}");
                var rootfs = new EmbedFieldBuilder()
                    .WithName($"RootFS: ")
                    .WithValue($"Total: {response.rootfs.total} \n Used: {response.rootfs.used} \n Free: {response.rootfs.free} \n Available: {response.rootfs.avail}");
                var embed = new EmbedBuilder
                    {
                    // Embed property can be set within object initializer
                    Title = node,
                    Description = response.pveversion,
                    Fields = new List<EmbedFieldBuilder>(){
                            cpu, cpusage, ram, rootfs
                        }              
                    };
                    await ReplyAsync(embed: embed.Build());
            }else{
                await ReplyAsync(text: "Connection to node failder");
            }
        }       
        [SlashCommand("vms", "Show all VM in Proxmox client")]
        public async Task Spawn([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password)
        {
            var client = new PveClient(ip);
            if (await client.Login(username, password))
            {
                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Please select a VM")
                    .WithCustomId("menu-1")
                    .WithMinValues(1)
                    .WithMaxValues(1);
                    var vmids_raw = await client.Cluster.Resources.Resources();
                    var vmids = vmids_raw.Response.data;
                    for(var i = 0; i < vmids.Count; i++){
                        try{
                            menuBuilder.AddOption(vmids[i].name,  $"ID:{vmids[i].vmid}/User:{username}/Password:{password}/IP:{ip}/Node:{node}",$"VM-{vmids[i].vmid}");
                        }catch(Exception ex){
                        }
                    }
                        
                    
                    var builder = new ComponentBuilder()
                                .WithSelectMenu(menuBuilder);
                            
                            await ReplyAsync("Choose a VM", components: builder.Build());
            }
         
            
}
    }
}
