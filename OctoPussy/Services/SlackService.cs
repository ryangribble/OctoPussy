using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OctoPussy.Models;

namespace OctoPussy.Services
{
    public class SlackService
    {
        private readonly HttpClient _httpClient;
        private readonly string _icon;
        private readonly string _userName;
        private string _slackToken;

        public SlackService(string webhookUrl, string userName, string icon, string slackToken)
        {
            if (webhookUrl == null) throw new ArgumentNullException("webhookUrl");
            if (userName == null) throw new ArgumentNullException("userName");
            if (icon == null) throw new ArgumentNullException("icon");
            if (slackToken == null) throw new ArgumentNullException("slackToken");

            _userName = userName;
            _icon = icon;
            _slackToken = slackToken;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(webhookUrl),
                Timeout = TimeSpan.FromSeconds(8)
            };
        }

        public bool VerifySlackToken(string token)
        {
            return this._slackToken == string.Empty || this._slackToken.ToLower() == token.ToLower();
        }

        public async Task<string> Send(string message, string channel)
        {
            var payload = new SlackMessageData
            {
                Text = message
            };

            return await Send(payload, channel);
        }

        public async Task<string> Send(SlackMessageData payload, string channel)
        {
            if (!channel.StartsWith("@") && !channel.StartsWith("#"))
                channel = "#" + channel;

            payload.UserName = _userName;
            payload.Icon_Emoji = _icon;
            payload.Channel = channel;

            var jsonContent = JsonConvert.SerializeObject(payload, Formatting.Indented,
                new JsonSerializerSettings { ContractResolver = new LowerCasePropertyNamesContractResolver() });

            HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("", content);

            return jsonContent;
        }
    }
}