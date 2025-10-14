using System.Threading;
using System.Threading.Tasks;
using OpenChat.ViewModels;

namespace OpenChat.Services
{
    public class NoteService
    {
        public NoteMessageViewModel MessageView { get; } = new NoteMessageViewModel();

        private CancellationTokenSource? cancellation;

        private async Task ShowCoreAsync(string msg, int timeout, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            MessageView.Text = msg;
            MessageView.Show = true;

            try
            {
                await Task.Delay(timeout, token);

                if (token.IsCancellationRequested)
                    return;

                MessageView.Show = false;
            }
            catch (TaskCanceledException) { }
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
            MessageView.Text = msg;
            MessageView.Show = true;
        }

        public void Close()
        {
            cancellation?.Cancel();

            MessageView.Show = false;
        }
    }
}
