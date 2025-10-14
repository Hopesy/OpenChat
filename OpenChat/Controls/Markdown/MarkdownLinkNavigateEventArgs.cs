using System;

namespace OpenChat.Controls.Markdown
{
    public class MarkdownLinkNavigateEventArgs : EventArgs
    {
        public MarkdownLinkNavigateEventArgs(string? link)
        {
            Link = link;
        }

        public string? Link { get; set; }
    }
}
