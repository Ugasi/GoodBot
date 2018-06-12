using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;

namespace GoodBot
{
    class Program
    {
        private static IConfigurationRoot Configuration;

        static void Main(string[] args)
        {
            InitConfiguration();
            new Program()
                .RunBot()
                .GetAwaiter()
                .GetResult();
        }

        private static void InitConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<Program>();
            Configuration = builder.Build();
        }

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;

        public async Task RunBot()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();

            string token = Configuration["DiscordToken"];

            client.Log += Log;

            await RegisterCommands();
            await client.LoginAsync(TokenType.Bot, token: token);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        public async Task RegisterCommands()
        {
            client.MessageReceived += HandleCommand;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommand(SocketMessage message)
        {
            var userMessage = message as SocketUserMessage;
            if (userMessage is null || userMessage.Author.IsBot) return;
            int argPos = 0;
            if(userMessage.HasStringPrefix("!", ref argPos) || userMessage.HasMentionPrefix(client.CurrentUser, ref argPos)) {
                var context = new SocketCommandContext(client, userMessage);
                var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess) {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}
