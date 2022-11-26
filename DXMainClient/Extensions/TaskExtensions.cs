using System;
using System.Threading.Tasks;

namespace DTAClient;

internal static class TaskExtensions
{
    /// <summary>
    /// Asynchronously awaits a <see cref="Task"/> and guarantees all exceptions are caught and handled when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> who's exceptions will be handled.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task HandleTaskAsync(this Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            PreStartup.HandleException(ex);
        }
    }

    /// <summary>
    /// Synchronously awaits a <see cref="Task"/> and guarantees all exceptions are caught and handled when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> who's exceptions will be handled.</param>
    public static void HandleTask(this Task task)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        => task.HandleTaskAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
}