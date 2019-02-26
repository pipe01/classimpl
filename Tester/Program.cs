using ClassImpl;
using System;
using System.Collections.Generic;

namespace Tester
{
    public interface ITest
    {
        string Test(string one, int two);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var m = new Implementer<ITest>();
            m.Method<string>(nameof(ITest.Test)).Callback(o =>
                "hello");

            var test = m.Finish();
            var ret = test.Test("sdasd", 42);
        }
    }
}
