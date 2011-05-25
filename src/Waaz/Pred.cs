using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Waaz
{
    public class Pred<T>
    {
        private readonly Func<T, bool> _pred;
        public Pred(Func<T, bool> pred)
        {
            _pred = pred;
        }

        public Func<T, bool> Func
        {
            get { return _pred; }
        }

        public static implicit operator Pred<T>(Func<T, bool> pred)
        {
            return new Pred<T>(pred);
        }

        public static implicit operator Func<T, bool>(Pred<T> p)
        {
            return p._pred;
        }

        public static bool operator true(Pred<T> p1)
        {
            return false;
        }

        public static bool operator false(Pred<T> p1)
        {
            return false;
        }

        public static Pred<T> operator &(Pred<T> p1, Pred<T> p2)
        {
            return new Pred<T>(c => p1._pred(c) && p2._pred(c));
        }

        public static Pred<T> operator |(Pred<T> p1, Pred<T> p2)
        {
            return new Pred<T>(c => p1._pred(c) || p2._pred(c));
        }

        public static Pred<T> Make(Func<T, bool> p)
        {
            return new Pred<T>(p);
        }
    }
}
