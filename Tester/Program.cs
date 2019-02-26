using ClassImpl;
using System;
using System.Collections.Generic;

namespace Tester
{
    public class ITest
    {
        public virtual string Test(string a, int b) => "original";
    }

    class Program
    {
        static void Main(string[] args)
        {
            var m = new Implementer<ITest>();
            m.Member(o => o.Test(It.IsAny<string>(), It.IsAny<int>())).Callback(o => "modified");

            var test = m.Finish();
            var ret = test.Test("123123", 42);
        }
    }
}
