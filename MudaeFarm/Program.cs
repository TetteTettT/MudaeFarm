﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octokit;

namespace MudaeFarm
{
    public static class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                   .ConfigureLogging((host, logger) =>
                    {
                        if (args.Contains("-v") || args.Contains("--verbose"))
                            logger.SetMinimumLevel(LogLevel.Trace);
                        else
                            logger.SetMinimumLevel(host.HostingEnvironment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information)
                                  .AddFilter(nameof(Disqord), LogLevel.Warning);

                        logger.AddFile($"log_{DateTime.Now.ToString("u").Replace(':', '.')}.txt");
                    })
                   .ConfigureAppConfiguration(config => config.Add(new DiscordConfigurationSource()))
                   .ConfigureServices((host, services) =>
                    {
                        // configuration
                        services.AddSingleton((IConfigurationRoot) host.Configuration)
                                .Configure<GeneralOptions>(host.Configuration.GetSection(GeneralOptions.Section))
                                .Configure<ClaimingOptions>(host.Configuration.GetSection(ClaimingOptions.Section))
                                .Configure<RollingOptions>(host.Configuration.GetSection(RollingOptions.Section))
                                .Configure<CharacterWishlist>(host.Configuration.GetSection(CharacterWishlist.Section))
                                .Configure<AnimeWishlist>(host.Configuration.GetSection(AnimeWishlist.Section))
                                .Configure<BotChannelList>(host.Configuration.GetSection(BotChannelList.Section))
                                .Configure<ReplyList>(host.Configuration.GetSection(ReplyList.Section))
                                .Configure<UserWishlistList>(host.Configuration.GetSection(UserWishlistList.Section));

                        // discord client
                        services.AddSingleton<IDiscordClientService, DiscordClientService>()
                                .AddTransient<IHostedService>(s => s.GetService<IDiscordClientService>());

                        services.AddSingleton<ICredentialManager, CredentialManager>();

                        // mudae services
                        services.AddSingleton<IMudaeUserFilter, MudaeUserFilter>()
                                .AddSingleton<IMudaeClaimCharacterFilter, MudaeClaimCharacterFilter>()
                                .AddSingleton<IMudaeClaimEmojiFilter, MudaeClaimEmojiFilter>()
                                .AddSingleton<IMudaeCommandHandler, MudaeCommandHandler>()
                                .AddSingleton<IMudaeOutputParser, EnglishMudaeOutputParser>() //todo: this needs to be configurable
                                .AddSingleton<IMudaeReplySender, MudaeReplySender>();

                        services.AddSingleton<IMudaeRoller, MudaeRoller>()
                                .AddTransient<IHostedService>(s => s.GetService<IMudaeRoller>());

                        services.AddSingleton<IMudaeClaimer, MudaeClaimer>()
                                .AddTransient<IHostedService>(s => s.GetService<IMudaeClaimer>());

                        // auto updater
                        services.AddHostedService<Updater>()
                                .AddSingleton<HttpClient>()
                                .AddSingleton(s => new GitHubClient(new ProductHeaderValue("MudaeFarm")));
                    });
    }
}