using ClassImpl;
using System;
using System.Collections.Generic;

namespace Tester
{
    public interface ITest
    {
        string Property { get; set; }

        //string Test(string one, int two);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var m = new Implementer<ITest>();
            m.Member(o => o.Property).Returns("eeeeeee");
            m.Setter(o => o.Property, Console.WriteLine);

            var test = m.Finish();
            test.Property = "asdasd";
        }
    }
}
