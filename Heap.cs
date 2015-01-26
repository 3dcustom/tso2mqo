using System;
using System.Collections.Generic;
using System.Text;

namespace Tso2MqoGui
{
    public class Heap<T>
    {
        public Dictionary<T, ushort> map = new Dictionary<T, ushort>();
        public List<T> ary = new List<T>();

        public void Clear()
        {
            map.Clear();
            ary.Clear();
        }

        public ushort Add(T v)
        {
            ushort n;

            if (map.TryGetValue(v, out n))
                return n;

            n = (ushort)ary.Count;
            map.Add(v, n);
            ary.Add(v);
            return n;
        }

        public int Count { get { return ary.Count; } }
        public ushort this[T index] { get { return map[index]; } }
    }
}
