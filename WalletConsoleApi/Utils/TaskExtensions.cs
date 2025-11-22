namespace WalletConsoleApi.Utils;

public static class TaskExtensions
{
    /// <summary>
    /// Waits for the given <paramref name="awaitableTask"/> to complete or for the <paramref name="cancellationToken"/> to be canceled.
    /// </summary>
    /// <throws cref="OperationCanceledException">The token is canceled</throws>
    public static async Task<T> WithCancellation<T>(this Task<T> awaitableTask, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); // OperationCanceledException

        var taskCompletionSourceOfCancellation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using (cancellationToken.Register(state =>
                     {
                         var localTaskCompletionSource = (TaskCompletionSource)state!;
                         localTaskCompletionSource.TrySetResult();
                     }, taskCompletionSourceOfCancellation))
        {
            var completed = await Task.WhenAny(awaitableTask, taskCompletionSourceOfCancellation.Task).ConfigureAwait(false);
            if (completed == taskCompletionSourceOfCancellation.Task)
                throw new OperationCanceledException(cancellationToken);

            return await awaitableTask.ConfigureAwait(false);
        }
    }
}
