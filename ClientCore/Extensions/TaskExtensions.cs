using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClientCore.Extensions;

public static class TaskExtensions
{
    /// <summary>
    /// Runs a <see cref="Task"/> and guarantees all exceptions are caught and handled even when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> who's exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task HandleTask(this Task task, bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }
    }

    /// <summary>
    /// Runs a <see cref="Task"/> and guarantees all exceptions are caught and handled even when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="task"/>'s return value.</typeparam>
    /// <param name="task">The <see cref="Task"/> who's exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task<T> HandleTask<T>(this Task<T> task, bool continueOnCapturedContext = false)
    {
        try
        {
            return await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }

        return default;
    }

    /// <summary>
    /// Executes a list of tasks and waits for all of them to complete and throws an <see cref="AggregateException"/> containing all exceptions from all tasks.
    /// When using <see cref="Task.WhenAll(IEnumerable{Task})"/> only the first thrown exception from a single <see cref="Task"/> may be observed.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="tasks"/>'s return value.</typeparam>
    /// <param name="tasks">The list of <see cref="Task"/>s who's exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task<T[]> WhenAllSafe<T>(IEnumerable<Task<T>> tasks, bool continueOnCapturedContext = false)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            return await whenAllTask.ConfigureAwait(continueOnCapturedContext);
        }
        catch
        {
            if (whenAllTask.Exception is null)
                throw;

            throw whenAllTask.Exception;
        }
    }

    /// <summary>
    /// Executes a list of tasks and waits for all of them to complete and throws an <see cref="AggregateException"/> containing all exceptions from all tasks.
    /// When using <see cref="Task.WhenAll(IEnumerable{Task})"/> only the first thrown exception from a single <see cref="Task"/> may be observed.
    /// </summary>
    /// <param name="tasks">The list of <see cref="Task"/>s who's exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task WhenAllSafe(IEnumerable<Task> tasks, bool continueOnCapturedContext = false)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            await whenAllTask.ConfigureAwait(continueOnCapturedContext);
        }
        catch
        {
            if (whenAllTask.Exception is null)
                throw;

            throw whenAllTask.Exception;
        }
    }

    /// <summary>
    /// Runs a <see cref="ValueTask"/> and guarantees all exceptions are caught and handled even when the <see cref="ValueTask"/> is not directly awaited.
    /// </summary>
    /// <param name="task">The <see cref="ValueTask"/> who's exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    public static async void HandleTask(this ValueTask task, bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }
    }
}