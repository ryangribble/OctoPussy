using System;
using System.Collections.Generic;
using System.Configuration;
using Autofac;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Serilog;
using OctoPussy.Services;

namespace OctoPussy
{
    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private string slackToken;
        private string slackWebhookUrl;
        private string slackUserName;
        private string slackUserIcon;

        private string githubAPIKey;
        private string githubURL;

        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
            Log.Logger = new LoggerConfiguration()
                            .ReadFrom.AppSettings()
                            .CreateLogger();

            slackToken = ConfigurationManager.AppSettings["slack:token"];
            slackWebhookUrl = ConfigurationManager.AppSettings["slack:webhookurl"];
            slackUserName = ConfigurationManager.AppSettings["slack:username"];
            slackUserIcon = ConfigurationManager.AppSettings["slack:icon"];

            githubAPIKey = ConfigurationManager.AppSettings["github:apikey"];
            githubURL = ConfigurationManager.AppSettings["github:url"];
        }

        protected override void ConfigureApplicationContainer(ILifetimeScope existingContainer)
        {
            var builder = new ContainerBuilder();

            builder
                .Register(p => new SlackService(slackWebhookUrl, slackUserName, slackUserIcon, slackToken));

            builder.Register(p => new GitHubStatusAPIService(githubAPIKey, githubURL));

            builder.Update(existingContainer.ComponentRegistry);
        }

        protected override void ConfigureRequestContainer(ILifetimeScope container, NancyContext context)
        {
            var builder = new ContainerBuilder();

            builder
                .Register(p => new SlackService(slackWebhookUrl, slackUserName, slackUserIcon, slackToken));

            builder.Update(container.ComponentRegistry);
        }
    }
}