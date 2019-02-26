using ClassImpl;
using System;
using System.Collections.Generic;

namespace Tester
{
    public interface ITest
    {
        string Property { get; }

        //string Test(string one, int two);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var m = new Implementer<ITest>();
            m.Member(o => o.Property).Returns("asdsad");

            var test = m.Finish();
            var ret = test.Property;
        }
    }
}
