using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using System.Collections.Generic;

namespace Proxmox.BOT
{
    public class Program
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true,
        };

        public Program()
        {
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "DC_")
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _services = new ServiceCollection()
                .AddSingleton(_configuration)
                .AddSingleton(_socketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .BuildServiceProvider();
        }
        static void Main(string[] args)
            => new Program().RunAsync()
                .GetAwaiter()
                .GetResult();

        public async Task MyMenuHandler(SocketMessageComponent arg)
        {
            var id = string.Join(", ", arg.Data.Values);
            var builder = new ComponentBuilder()
                .WithButton("Shutdown", $"shutdown{id}", ButtonStyle.Danger)
                .WithButton("Stop", $"stop{id}", ButtonStyle.Danger)
                .WithButton("Reboot", $"reboot{id}", ButtonStyle.Success)
                .WithButton("Reset", $"reset{id}", ButtonStyle.Success)
                .WithButton("Status", $"status{id}", ButtonStyle.Success);

            await arg.RespondAsync($"VM {id.Split("/")[0].Replace("ID:", "")} selected: Choose an action:", components: builder.Build());
        }
        public async Task MyButtonHandler(SocketMessageComponent component)
        {   
            var info = component.Data.CustomId;
           if(info.Contains("status")){
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
                    // Embed property can be set within object initializer
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
        public async Task RunAsync()
        {
            
            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;
            client.SelectMenuExecuted += MyMenuHandler;
            client.ButtonExecuted += MyButtonHandler;

            // Here we can initialize the service that will register and execute our commands
            await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();
            var config_parser = new Config.Config();
            var config = config_parser.Parse();
            // Bot token can be provided from the Configuration object we set up earlier
            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            // Never quit the program until manually forced to.
            await Task.Delay(Timeout.Infinite);
        }

        private async Task LogAsync(LogMessage message)
            => Console.WriteLine(message.ToString());

        public static bool IsDebug()
        {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
        }
    }
}
