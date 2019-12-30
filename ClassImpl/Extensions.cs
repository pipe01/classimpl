using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassImpl
{
    internal static class Extensions
    {
        public static IEnumerable<Type> GetTypeAndInterfaces(this Type type)
            => new[] { type }.Concat(type.GetInterfaces());
    }
}
