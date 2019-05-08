using ClassImpl;
using System;
using System.Diagnostics;
using System.Linq;

namespace Tester
{
    public interface ITest
    {
        void NoReturn();
        string YesReturn();
        string YesReturn2();
        int ReturnWTF();
    }

    class Program
    {
        static void Main(string[] args)
        {
            ITest obj = null;

            var impl = new Implementer(typeof(ITest), typeof(bool));
            //impl.Getter(impl.Properties[0]).Callback(o => o["__data"]);
            //impl.Setter(impl.Properties[0], Console.WriteLine);
            impl.HandleAll((m, d) => "hello " + m.Name, true);

            obj = (ITest)impl.Finish(true);

            var a = obj.YesReturn();
            a = obj.YesReturn2();
            obj.ReturnWTF();
        }

        private static void Bench(Action action)
        {
            const int count = 500;

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                action();
            }

            sw.Stop();

            Console.WriteLine("Time per iteration: " + sw.Elapsed / count);
        }
    }
}
