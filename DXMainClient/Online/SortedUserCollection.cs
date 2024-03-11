using System;
using System.Collections.Generic;

namespace DTAClient.Online;

/// <summary>
/// A custom collection that aims to provide quick insertion,
/// removal and lookup operations while always keeping the list sorted
/// by combining Dictionary and LinkedList.
/// </summary>
public class SortedUserCollection<T> : IUserCollection<T>
{
    public SortedUserCollection(Func<T, T, int> userComparer)
    {
        dictionary = [];
        linkedList = new LinkedList<T>();
        this.userComparer = userComparer;
    }

    private readonly Dictionary<string, LinkedListNode<T>> dictionary;
    private readonly LinkedList<T> linkedList;

    private readonly Func<T, T, int> userComparer;

    public int Count => dictionary.Count;

    public void Add(string username, T item)
    {
        if (linkedList.Count == 0)
        {
            LinkedListNode<T> node = linkedList.AddFirst(item);
            dictionary.Add(username.ToLower(), node);
            return;
        }

        LinkedListNode<T> currentNode = linkedList.First;
        while (true)
        {
            if (userComparer(currentNode.Value, item) > 0)
            {
                LinkedListNode<T> node = linkedList.AddBefore(currentNode, item);
                dictionary.Add(username.ToLower(), node);
                break;
            }

            if (currentNode.Next == null)
            {
                LinkedListNode<T> node = linkedList.AddAfter(currentNode, item);
                dictionary.Add(username.ToLower(), node);
                break;
            }

            currentNode = currentNode.Next;
        }
    }

    public bool Remove(string username)
    {
        if (dictionary.TryGetValue(username.ToLower(), out LinkedListNode<T> node))
        {
            linkedList.Remove(node);
            _ = dictionary.Remove(username.ToLower());
            return true;
        }

        return false;
    }

    public T Find(string username)
    {
        return dictionary.TryGetValue(username.ToLower(), out LinkedListNode<T> node) ? node.Value : default;
    }

    public void Reinsert(string username)
    {
        T existing = Find(username.ToLower());
        if (existing == null)
        {
            return;
        }

        _ = Remove(username);
        Add(username, existing);
    }

    public void Clear()
    {
        linkedList.Clear();
        dictionary.Clear();
    }

    public LinkedListNode<T> GetFirst()
    {
        return linkedList.First;
    }

    public void DoForAllUsers(Action<T> action)
    {
        LinkedListNode<T> current = linkedList.First;
        while (current != null)
        {
            action(current.Value);
            current = current.Next;
        }
    }
}