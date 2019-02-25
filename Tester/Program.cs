using ClassImpl;
using System;

namespace Tester
{
    public interface ITest
    {
        void Test();
    }

    class Program
    {
        static void Main(string[] args)
        {
            var m = new Implementer<ITest>();
            m.Method(nameof(ITest.Test)).Callback(() => Console.WriteLine("Called!"));

            var test = m.Finish();
            test.Test();
        }
    }
}
