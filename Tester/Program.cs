using ClassImpl;
using System;
using System.Diagnostics;
using System.Linq;

namespace Tester
{
    public interface ITest
    {
        string Prop1 { get; set; }
        int Prop2 { get; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ITest obj = null;

            var impl = new Implementer(typeof(ITest), typeof(string));
            impl.Getter(impl.Properties[0]).Callback(o => o["__data"]);
            impl.Setter(impl.Properties[0], Console.WriteLine);
            impl.Getter(impl.Properties[1]).Returns(123);

            obj = (ITest)impl.Finish("hello");

            //ClassUtils.Copy(obj, "new data");
            Bench(() => ClassUtils.Copy(obj, "new data"));

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
