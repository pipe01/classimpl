using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ClassImpl
{
    internal static class ILUtils
    {
        public static void CreateDicAndAddValues(this ILGenerator il, MethodInfo method)
        {
            il.DeclareLocal(typeof(IDictionary<string, object>));
            il.Emit(OpCodes.Newobj, typeof(Dictionary<string, object>).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0);

            int i = 1;
            foreach (var item in method.GetParameters())
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldstr, item.Name);
                il.Emit(OpCodes.Ldarg, i++);

                if (item.ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, item.ParameterType);

                il.EmitCall(OpCodes.Callvirt,
                    typeof(IDictionary<string, object>).GetMethod("Add", new[] { typeof(string), typeof(object) }), null);
            }
        }
    }
}
