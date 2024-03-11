using System;
using System.Collections.Generic;

namespace DTAClient.Online;

/// <summary>
/// A custom collection that aims to provide quick insertion,
/// removal and lookup operations by using a dictionary. Does not
/// keep the list sorted.
/// </summary>
public class UnsortedUserCollection<T> : IUserCollection<T>
{
    private readonly Dictionary<string, T> dictionary = [];

    public int Count => dictionary.Count;

    public void Add(string username, T item)
    {
        dictionary.Add(username.ToLower(), item);
    }

    public void Clear()
    {
        dictionary.Clear();
    }

    public void DoForAllUsers(Action<T> action)
    {
        Dictionary<string, T>.ValueCollection values = dictionary.Values;

        foreach (T value in values)
        {
            action(value);
        }
    }

    public T Find(string username)
    {
        return dictionary.TryGetValue(username.ToLower(), out T value) ? value : default;
    }

    public LinkedListNode<T> GetFirst()
    {
        throw new NotImplementedException();
    }

    public void Reinsert(string username)
    {
        throw new NotImplementedException();
    }

    public bool Remove(string username)
    {
        return dictionary.Remove(username.ToLower());
    }
}