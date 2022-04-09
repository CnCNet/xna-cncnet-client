using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.Online
{
    /// <summary>
    /// A custom collection that aims to provide quick insertion,
    /// removal and lookup operations while always keeping the list sorted
    /// by combining Dictionary and LinkedList.
    /// </summary>
    public class SortedUserCollection<T> : IUserCollection<T>
    {
        public SortedUserCollection(Func<T, T, int> userComparer)
        {
            dictionary = new Dictionary<string, LinkedListNode<T>>();
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
                var node = linkedList.AddFirst(item);
                dictionary.Add(username.ToLower(), node);
                return;
            }

            var currentNode = linkedList.First;
            while (true)
            {
                if (userComparer(currentNode.Value, item) > 0)
                {
                    var node = linkedList.AddBefore(currentNode, item);
                    dictionary.Add(username.ToLower(), node);
                    break;
                }

                if (currentNode.Next == null)
                {
                    var node = linkedList.AddAfter(currentNode, item);
                    dictionary.Add(username.ToLower(), node);
                    break;
                }

                currentNode = currentNode.Next;
            }
        }

        public List<T> ToList() => linkedList.ToList();

        public bool Remove(string username)
        {
            if (dictionary.TryGetValue(username.ToLower(), out var node))
            {
                linkedList.Remove(node);
                dictionary.Remove(username.ToLower());
                return true;
            }

            return false;
        }

        public T Find(string username)
        {
            if (dictionary.TryGetValue(username.ToLower(), out var node))
                return node.Value;

            return default(T);
        }

        public void Reinsert(string username)
        {
            var existing = Find(username.ToLower());
            if (existing == null)
                return;

            Remove(username);
            Add(username, existing);
        }

        public void Clear()
        {
            linkedList.Clear();
            dictionary.Clear();
        }

        public LinkedListNode<T> GetFirst() => linkedList.First;

        public void DoForAllUsers(Action<T> action)
        {
            var current = linkedList.First;
            while (current != null)
            {
                action(current.Value);
                current = current.Next;
            }
        }
    }
}
