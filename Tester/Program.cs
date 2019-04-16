using ClassImpl;
using System;
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
            var m = new Implementer(typeof(ITest), typeof(Func<string>));
            m.Getter(m.Properties[0]).Callback(o => (o["__data"] as Func<string>)());
            m.Setter(m.Properties[0], (o, data) =>
            Console.WriteLine(o));
            m.Getter(m.Properties[1]).Returns(123);

            int i = 0;
            var obj = (ITest)m.Finish((Func<string>)(() => (i++).ToString()));
            var a = obj.Prop1;
            obj.Prop1 = "what's up";

            Implementer.SetData(obj, (Func<string>)(() => (i += 2).ToString()));
        }

        public object Test() => 123;
    }
}
