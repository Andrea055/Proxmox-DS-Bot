using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using System.Collections.Generic;
using Newtonsoft.Json;
using Proxmox.Model;
using Discord.WebSocket;
using Proxmox.Permission;
using System.Linq;

namespace Proxmox.BOT
{
    // Interation modules must be public and inherit from an IInterationModuleBase
    public class ProxbotModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private readonly InteractionHandler _handler;

        // Constructor injection is also a valid way to access the dependencies
        public ProxbotModule(InteractionHandler handler)
        {
            _handler = handler;
        }

        [SlashCommand("ping", "Pings the bot and returns its latency.")]
        public async Task GreetUserAsync()
            => await RespondAsync(text: $":ping_pong: It took me {Context.Client.Latency}ms to respond to you!", ephemeral: true);

        [SlashCommand("specs", "Gets Specification of server")]
        public async Task Specs([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            
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
                    Title = node,
                    Description = response.pveversion,
                    Fields = new List<EmbedFieldBuilder>(){
                            cpu, cpusage, ram, rootfs
                        }              
                    };
                    await RespondAsync(embed: embed.Build());
            }else{
                await RespondAsync(text: "Failed to connect to server");      
            }
        }       
        [SlashCommand("vms", "Show all VM in Proxmox client")]
        public async Task VMs([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password)
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
                        }catch{

                        }
                    }
                    var builder = new ComponentBuilder()
                                .WithSelectMenu(menuBuilder);
                            
                    await RespondAsync("Choose a VM", components: builder.Build());
            }else{
                await RespondAsync(text: "Failed to connect to server");
            }        
        }
        [SlashCommand("netstat", "Get statistics from all network VM")]
        public async Task NetStat([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            var client = new PveClient(ip);
                if (await client.Login(username, password)){
                    var netstat_raw = await client.Nodes[node].Netstat.Netstat();
                    var netstat = netstat_raw.Response.data;
                    var fields = new List<EmbedFieldBuilder>();
                    foreach(dynamic vm in netstat){
                        string vmstring = JsonConvert.SerializeObject(vm);
                        vmstring = vmstring.Replace("in", "innet");
                        vmstring = vmstring.Replace("out", "outnet");
                        Netstat vmtrue = JsonConvert.DeserializeObject<Netstat>(vmstring);
                        fields.Add(new EmbedFieldBuilder()
                            .WithName($"Network in - VM{vm.vmid}")
                            .WithValue(vmtrue.innet)
                        );
                        fields.Add(new EmbedFieldBuilder()
                            .WithName($"Network out - VM{vm.vmid}")
                            .WithValue(vmtrue.outnet)
                        );
                    }
                    var embed = new EmbedBuilder
                        {
                        Title = $"Network usage in {node}",
                        Fields = fields           
                        };   
                    await RespondAsync(embed: embed.Build());
            }else{
                await RespondAsync(text: "Failed to connect to server");
            }

        }
        [SlashCommand("getdns", "Get the DNS on Proxmox node")]
        public async Task GetDNS([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
                var client = new PveClient(ip);
                if (await client.Login(username, password)){
                    var dns_raw = await client.Nodes[node].Dns.Dns();
                    var dns = dns_raw.Response.data;
                    await RespondAsync(text: $"The DNS for node {node} is {JsonConvert.SerializeObject(dns)}");
                }else{
                    await RespondAsync(text: "Failed to connect to server");
                }
            
        }
        [SlashCommand("setdns", "Set the DNS on Proxmox node")]
        public async Task SetDNS([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password,
        [Summary(description: "Search domain")]string search, [Summary(description: "DNS1 IP")] string dns1, [Summary(description: "DNS2 IP")] string dns2,
        [Summary(description: "DNS3 IP")] string dns3){
                var user = Context.User as SocketGuildUser;
                var checker = new Checker();
                if(checker.isAdmin(user)){
                    var client = new PveClient(ip);
                    if (await client.Login(username, password)){
                        await client.Nodes[node].Dns.UpdateDns(search, dns1, dns2, dns3);
                        await RespondAsync(text: "DNS changed successfully");
                    }else{
                        await RespondAsync(text: "Failed to connect to server");
                    }
                }else{
                    await RespondAsync(text: "You don't have the permission to do that");
                }
        }
        [SlashCommand("startall", "Start all VMs")]
        public async Task StartAll([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            var checker = new Checker();
            var user = Context.User as SocketGuildUser;
            if(checker.isAdmin(user)){
                    var client = new PveClient(ip);
                    if (await client.Login(username, password)){
                        await client.Nodes[node].Startall.Startall();
                        await RespondAsync(text: "All VMs are started correctly");
                    }else{
                        await RespondAsync(text: "Failed to connect to server");
                    }
                }else{
                    await RespondAsync(text: "You don't have the permission to do that");
                }
            }
        [SlashCommand("stopall", "Stop all VMs")]
        public async Task StopAll([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            var checker = new Checker();
            var user = Context.User as SocketGuildUser;
            if(checker.isAdmin(user)){
                    var client = new PveClient(ip);
                    if (await client.Login(username, password)){
                        await client.Nodes[node].Stopall.Stopall();
                        await RespondAsync(text: "All VMs are stopped correctly");
                    }else{
                        await RespondAsync(text: "Failed to connect to server");
                    }
                }else{
                    await RespondAsync(text: "You don't have the permission to do that");
                }
            }
        [SlashCommand("syslog", "Read the system log")]
        public async Task ReadLogs([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip, 
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            var client = new PveClient(ip);
            if (await client.Login(username, password)){
                var log_raw = await client.Nodes[node].Syslog.Syslog();
                var log = log_raw.Response.data;
                var all_log_raw = new List<string>();
                foreach (var lograw in log){
                    all_log_raw.Add(lograw.t); 
                }
                await RespondAsync(text: string.Join("\n", all_log_raw));
            }else{
                await RespondAsync(text: "Failed to connect to server");
            }
        }
        [SlashCommand("getsubscription", "Get the subscription information")]
        public async Task ReadSubscription([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip,
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            var client = new PveClient(ip);
            if (await client.Login(username, password)){
                var sub_raw = await client.Nodes[node].Subscription.Get();
                var sub = sub_raw.Response.data;
                await RespondAsync(text: sub.message);
            }else{
                await RespondAsync(text: "Failed to connect to server");
            } 
        }
        [SlashCommand("setsubscription", "Set the subscription key")]
        public async Task SetSubscription([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip,
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password,
        [Summary(description: "Subscription key")]string subscriptionkey){
            var user = Context.User as SocketGuildUser;
            var checker = new Checker();
            if(checker.isAdmin(user)){
                var client = new PveClient(ip);
                if (await client.Login(username, password)){
                    await client.Nodes[node].Subscription.Set(subscriptionkey);
                    await RespondAsync(text: "Key updated!");
                }else{
                    await RespondAsync(text: "Failed to connect to server");
                } 
            }else{
                await RespondAsync(text: "You don't have the permission to do that");
            }
        }
        [SlashCommand("deletesubscription", "Delete the subscription key")]
        public async Task DeleteSubscription([Summary(description: "Node name")]string node,[Summary(description: "IP of Proxmox server")]string ip,
        [Summary(description: "Username of Proxmox server")]string username,[Summary(description: "Password of Proxmox server")]string password){
            var user = Context.User as SocketGuildUser;
            var checker = new Checker();
            if(checker.isAdmin(user)){
                var client = new PveClient(ip);
                if (await client.Login(username, password)){
                    await client.Nodes[node].Subscription.Delete();
                    await RespondAsync(text: "Key deleted!");
                }else{
                    await RespondAsync(text: "Failed to connect to server");
                } 
            }else{
                await RespondAsync(text: "You don't have the permission to do that");
            }
        }
    }
        
}
