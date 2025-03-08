using System.Text;

namespace System.Threading.Tasks;

internal static class CancellationTokenSourceExtensions
{
    public static Task CancelAsync(this CancellationTokenSource cancellationTokenSource)
    {
        if (cancellationTokenSource is null)
        {
            throw new ArgumentNullException(nameof(cancellationTokenSource));
        }

        cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }
}