using System.Configuration;
using System.Web.UI.WebControls;
using Nancy;
using Nancy.ModelBinding;
using OctoPussy.Models;
using OctoPussy.Services;
using System;
using System.Collections.Generic;
using Octokit;

namespace OctoPussy.Modules
{
    public class IndexModule : NancyModule
    {
        public IndexModule(SlackService slack, GitHubStatusAPIService gitservice)
        {
            var channel = ConfigurationManager.AppSettings["test:channel"];
            var url = ConfigurationManager.AppSettings["octopussy:url"];

            Get["/"] = parameters =>
            {
                return View["index"];
            };

            Get["/test/slack", true] = async (x, ct) =>
            {
                await slack.Send("hello world", channel);

                return View["index"];
            };

            Get["/test/github/prsummary/{owner}/{repo}", true] = async (x, ct) =>
            {
                string owner = x.owner;
                string repo = x.repo;
                var summary = await gitservice.GetAllPullRequestStatus(owner, repo);
                var payload = GenerateSlackMessagePR(await gitservice.GetRepositoryURL(owner, repo), owner, repo, "Nancy-Test", summary);

                return await slack.Send(payload, channel);
            };

            Get["/github/status/{owner}/{repo}/{reference}/{status?pending}/{context?all}", true] = async (x, ct) =>
            {
                bool all = x.context == "all";

                string owner = x.owner;
                string repo = x.repo;
                string reference = x.reference;

                var status = (Octokit.CommitState)Enum.Parse(typeof(Octokit.CommitState), x.status, true);

                if (all)
                {
                    var statusChecks = new List<Tuple<string, string>>();
                    statusChecks.Add(new Tuple<string, string>("build", "Verify build has had no errors"));
                    statusChecks.Add(new Tuple<string, string>("test-unit", "Ensure unit tests have passed"));
                    statusChecks.Add(new Tuple<string, string>("test-meerkat", "Ensure Meerkat tests have passed"));
                    statusChecks.Add(new Tuple<string, string>("rally", "Review rally items"));
                    statusChecks.Add(new Tuple<string, string>("code-review", "Organise code review by a team member"));

                    foreach (var check in statusChecks)
                    {
                        await gitservice.SetStatus(owner, repo, reference, check.Item1, status, check.Item2, GeneratePullRequestSuccessURL(url, owner, repo, reference, check.Item1, CommitState.Success));
                    }
                }
                else
                {
                    await gitservice.SetStatus(owner, repo, reference, x.context, status, "", GeneratePullRequestSuccessURL(url, owner, repo, reference, x.context, Octokit.CommitState.Success));
                }

                return View["index"];
            };

            Post["/slack", true] = async (x, ct) =>
            {
                var data = this.Bind<SlackPostData>();

                if (data != null)
                {
                    if (slack.VerifySlackToken(data.token))
                    {
                        if (data.command.StartsWith("github"))
                        {
                            // Do stuff then post result to slack
                            var split = data.text.Split('/');
                            if (split.Length >= 2)
                            {
                                string owner = split[0];
                                string repo = split[1];

                                var summary = await gitservice.GetAllPullRequestStatus(owner, repo);
                                var payload = GenerateSlackMessagePR(await gitservice.GetRepositoryURL(owner, repo), owner, repo, data.user_name, summary);

                                await slack.Send(payload, data.channel_name);

                                return "Response sent to slack";
                            }
                        }
                    }
                    else
                    {
                        return "Hey, " + data.user_name + ", your request was not validated as the slack Token didnt match (" + data.token + ")";
                    }
                }

                return "I don't know what's going on here, Sorry";
            };
        }

        private SlackMessageData GenerateSlackMessagePR(string repoURL, string owner, string repo, string requestedBy, List<dynamic> summary)
        {
            var payload = new SlackMessageData();
            payload.Text = string.Format("Responding to Request for PR status of <{0}/{1}/{2}|{1}/{2}> by @{3}\n\n\n", repoURL, owner, repo, requestedBy);

            if (summary.Count <= 0)
            {
                var attachment = new SlackAttachment
                {
                    Color = "good",
                    Pretext = "No PR statuses exist in repo"
                };
                payload.Attachments.Add(attachment);
            }

            foreach (var entry in summary)
            {
                var attachment = new SlackAttachment
                {
                    Color = entry.State.ToString().ToLower() == "success" ? "good" : "danger",
                    Pretext = string.Format("<{0}|Pull Request #{1}> - {2}\n\n", entry.URL, entry.Number, entry.State)
                };
                attachment.Text += String.Format("Pending: {0}\t", entry.Pending);
                attachment.Text += String.Format("Error:   {0}\t", entry.Error);
                attachment.Text += String.Format("Failure: {0}\t", entry.Failure);
                attachment.Text += String.Format("Success: {0}\n", entry.Success);

                payload.Attachments.Add(attachment);
            }

            return payload;
        }

        private string GeneratePullRequestSuccessURL(string selfUrl, string owner, string repo, string reference, dynamic context, CommitState status)
        {
            return String.Format("{0}/github/status/{1}/{2}/{3}/{4}/{5}", selfUrl, owner, repo, reference, status, context);
        }
    }
}