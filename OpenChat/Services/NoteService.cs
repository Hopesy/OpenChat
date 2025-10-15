using System.Threading;
using System.Threading.Tasks;
using OpenChat.Models;
using OpenChat.ViewModels.Pages;

namespace OpenChat.Services
{
    public class NoteService
    {
        public NoteMessageModel Message { get; } = new NoteMessageModel();
        private CancellationTokenSource? cancellation;
        private async Task ShowCoreAsync(string msg, int timeout, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;
            Message.Text = msg;
            Message.Show = true;
            try
            {
                await Task.Delay(timeout, token);
                if (token.IsCancellationRequested)
                    return;
                Message.Show = false;
            }
            catch (TaskCanceledException)
            {
            }
        }
        public Task ShowAndWaitAsync(string msg, int timeout)
        {
            cancellation?.Cancel();
            cancellation = new CancellationTokenSource();
            return ShowCoreAsync(msg, timeout, cancellation.Token);
        }
        public void Show(string msg, int timeout)
        {
            cancellation?.Cancel();
            cancellation = new CancellationTokenSource();
            _ = ShowCoreAsync(msg, timeout, cancellation.Token);
        }
        public void Show(string msg)
        {
            Message.Text = msg;
            Message.Show = true;
        }
        public void Close()
        {
            cancellation?.Cancel();
            Message.Show = false;
        }
    }
}