using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ClassImpl
{
    internal static class ILUtils
    {
        private static readonly MethodInfo DictionaryAdd = typeof(IDictionary<string, object>).GetMethod("Add", new[] { typeof(string), typeof(object) });

        public static void CreateDicAndAddValues(this ILGenerator il, MethodInfo method, FieldInfo dataField)
        {
            il.DeclareLocal(typeof(IDictionary<string, object>));
            il.Emit(OpCodes.Newobj, typeof(Dictionary<string, object>).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0);

            if (dataField != null)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldstr, "__data");
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, dataField);

                if (dataField.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, dataField.FieldType);

                il.EmitCall(OpCodes.Callvirt, DictionaryAdd, null);
            }

            int i = 1;
            foreach (var item in method.GetParameters())
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldstr, item.Name);
                il.Emit(OpCodes.Ldarg, i++);

                if (item.ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, item.ParameterType);

                il.EmitCall(OpCodes.Callvirt, DictionaryAdd, null);
            }
        }
    }
}
