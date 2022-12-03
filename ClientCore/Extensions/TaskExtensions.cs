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
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task HandleTask(this Task task)
    {
        try
        {
            await task;
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
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task<T> HandleTask<T>(this Task<T> task)
    {
        try
        {
            return await task;
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
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task<T[]> WhenAllSafe<T>(IEnumerable<Task<T>> tasks)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            return await whenAllTask;
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
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task WhenAllSafe(IEnumerable<Task> tasks)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            await whenAllTask;
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
    /// <returns>Returns a <see cref="ValueTask"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async ValueTask HandleTask(this ValueTask task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }
    }

    /// <summary>
    /// Runs a <see cref="ValueTask"/> and guarantees all exceptions are caught and handled even when the <see cref="ValueTask"/> is not directly awaited.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="task"/>'s return value.</typeparam>
    /// <param name="task">The <see cref="ValueTask"/> who's exceptions will be handled.</param>
    /// <returns>Returns a <see cref="ValueTask"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async ValueTask<T> HandleTask<T>(this ValueTask<T> task)
    {
        try
        {
            return await task;
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }

        return default;
    }
}