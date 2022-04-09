using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.Online
{
    /// <summary>
    /// A custom collection that aims to provide quick insertion,
    /// removal and lookup operations by using a dictionary. Does not
    /// keep the list sorted.
    /// </summary>
    public class UnsortedUserCollection<T> : IUserCollection<T>
    {
        private Dictionary<string, T> dictionary = new Dictionary<string, T>();

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
            var values = dictionary.Values;
            
            foreach (T value in values)
            {
                action(value);
            }
        }

        public T Find(string username)
        {
            if (dictionary.TryGetValue(username.ToLower(), out T value))
                return value;

            return default(T);
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

        public List<T> ToList() => dictionary.Values.ToList();
    }
}
