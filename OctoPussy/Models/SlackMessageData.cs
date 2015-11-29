using System.Collections.Generic;

namespace OctoPussy.Models
{
    public class SlackMessageData
    {
        public string Text { get; set; }
        public string UserName { get; set; }
        public string Icon_Emoji { get; set; }
        public string Channel { get; set; }
        public List<SlackAttachment> Attachments { get; set; }

        public SlackMessageData()
        {
            Attachments = new List<SlackAttachment>();
        }
    }
}