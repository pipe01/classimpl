using ClassImpl;
using System;
using System.Diagnostics;
using System.Linq;

namespace Tester
{
    public interface ITest
    {
        bool On { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ITest obj = null;

            var impl = new Implementer(typeof(ITest), typeof(bool));
            impl.Getter(impl.Properties[0]).Callback(o => o["__data"]);
            impl.Setter(impl.Properties[0], Console.WriteLine);

            obj = (ITest)impl.Finish(true);

            //ClassUtils.Copy(obj, "new data");
            Bench(() => ClassUtils.Copy(obj, false));

            Console.ReadKey(true);
            Main(args);
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
