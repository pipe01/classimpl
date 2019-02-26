using ClassImpl;
using System;
using System.Collections.Generic;

namespace Tester
{
    public class ITest
    {
        public virtual void Test(string a, int b) { }
        public virtual void Test2(string a, int b) { }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var m = new Implementer<ITest>();
            m.HandleAll((method, ar2gs) =>
            {

            });

            var test = m.Finish();
            test.Test("123123", 42);
        }
    }
}
