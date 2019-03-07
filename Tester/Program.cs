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
            var m = new Implementer(typeof(ITest));
            m.Getter(m.Properties[0]).Callback(() => "hello");
            m.Setter(m.Properties[0], Console.WriteLine);
            m.Getter(m.Properties[1]).Returns(123);

            var obj = (ITest)m.Finish();
            var a = obj.Prop2;
            obj.Prop1 = "what's up";
        }

        public object Test() => 123;
    }
}
