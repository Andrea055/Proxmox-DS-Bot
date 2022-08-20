using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Proxmox.Bot.Handler;
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
                .WithButton("Status", $"status{id}", ButtonStyle.Success)
                .WithButton("Config", $"config{id}", ButtonStyle.Success);

            await arg.RespondAsync($"VM {id.Split("/")[0].Replace("ID:", "")} selected: Choose an action:", components: builder.Build());
        }
        public async Task MyButtonHandler(SocketMessageComponent component)
        {   
            var info = component.Data.CustomId;
           if(info.Contains("status")){
            var status_handler = new Status();
            status_handler.Handler(component, info);
           }else if(info.Contains("stop")){
            var stop_handler = new Stop();
            stop_handler.Handler(component, info);
           }else if(info.Contains("reboot")){
            var reboot_handler = new Reboot();
            reboot_handler.Handler(component, info);
           }else if(info.Contains("reset")){
            var reset_handler = new Reset();
            reset_handler.Handler(component, info);
           }else if(info.Contains("shutdown")){
            var shutdown_handler = new Shutdown();
            shutdown_handler.Handler(component, info);
           }else if(info.Contains("config")){
            var config_handler = new Bot.Handler.Config();
            config_handler.Handler(component, info);
            }else{
                await component.RespondAsync("Exception during the execution of call, contact the BOT creator or owner");
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
