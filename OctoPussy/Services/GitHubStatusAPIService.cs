using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace OctoPussy.Services
{
    public class GitHubStatusAPIService
    {
        private readonly GitHubClient _client;

        public GitHubStatusAPIService(string apiKey, string url = null)
        {
            _client = String.IsNullOrEmpty(url) ? new GitHubClient(new ProductHeaderValue("OctoPussy")) : new GitHubClient(new ProductHeaderValue("OctoPussy"), new Uri(url));
            _client.Credentials = new Credentials(apiKey);
        }

        public async Task<string> GetRepositoryURL(string owner, string repository)
        {
            var repo = await _client.Repository.Get(owner, repository);
            return repo.HtmlUrl;
        }
        public async Task<List<dynamic>> GetAllPullRequestStatus(string owner, string repository)
        {
            var result = new List<dynamic>();
            var pulls = await _client.PullRequest.GetAllForRepository(owner, repository);
            foreach (var pull in pulls)
            {
                string reference = pull.Head.Sha;
                var combined = await _client.Repository.CommitStatus.GetCombined(owner, repository, reference);
                var statuses = await _client.Repository.CommitStatus.GetAll(owner, repository, reference);
                int pending = combined.Statuses.Count(x => x.State == CommitState.Pending);
                int error = combined.Statuses.Count(x => x.State == CommitState.Error);
                int failure = combined.Statuses.Count(x => x.State == CommitState.Failure);
                int success = combined.Statuses.Count(x => x.State == CommitState.Success);
                dynamic myPR = new ExpandoObject();
                myPR.URL = pull.HtmlUrl;
                myPR.Number = pull.Number;
                myPR.State = combined.State;
                myPR.Pending = pending;
                myPR.Error = error;
                myPR.Failure = failure;
                myPR.Success = success;

                result.Add(myPR);
            }

            return result;
        }

        public async Task<string> SetStatus(string owner, string repository, string reference, string context, CommitState state, string description, string targetUrl)
        {
            if (String.IsNullOrEmpty(description))
            {
                // Get current status with the same context to preserve description
                CombinedCommitStatus combined = await _client.Repository.CommitStatus.GetCombined(owner, repository, reference);
                var status = combined.Statuses.First(s => s.Context == context);
                description = status.Description;
            }

            NewCommitStatus newStatus = new NewCommitStatus()
            {
                Context = context,
                State = state,
                Description = description,
                TargetUrl = new Uri(targetUrl)
            };
            try
            {
                var rslt = await _client.Repository.CommitStatus.Create(owner, repository, reference, newStatus);
                return rslt.Id.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}