using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TExecutor = System.Action<System.Reflection.MethodBase, object[]>;

namespace ClassImpl
{
    public class Implementer<TInterface>
    {
        private readonly Type Type = typeof(TInterface);
        private readonly PropertyInfo[] Properties;
        private readonly MethodInfo[] Methods;

        internal readonly TypeBuilder Builder;
        internal readonly IDictionary<FieldBuilder, object> TypeFields = new Dictionary<FieldBuilder, object>();

        public Implementer()
        {
            if (!typeof(TInterface).IsInterface)
                throw new Exception(nameof(TInterface) + " must be an interface type!");

            Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Methods = Type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var interfaceType = typeof(TInterface);
            string name = interfaceType.Name;

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = "ProxyAssembly" + name }, AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("ProxyModule" + name);
            Builder = mb.DefineType("<>Impl" + name, TypeAttributes.Class, null, new[] { interfaceType });
        }

        /// <summary>
        /// Returns a method builder, representing a method identified by <paramref name="methodName"/>
        /// and optionally a number of <paramref name="parameters"/>.
        /// </summary>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="parameters">The type of the method's parameters.
        /// Must be passed if there is more than one method with the same name.</param>
        public IMethodBuilder Method(string methodName, params Type[] parameters)
        {
            return new MethodBuilder<TInterface>(GetMethod(methodName, parameters), this);
        }

        /// <summary>
        /// Returns a method builder, representing a method that returns a value of type <typeparamref name="TReturnType"/>
        /// identified by <paramref name="methodName"/> and optionally a number of <paramref name="parameters"/>.
        /// </summary>
        /// <typeparam name="TReturnType">The type of the value that the method returns.</typeparam>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="parameters">The type of the method's parameters.
        /// Must be passed if there is more than one method with the same name.</param>
        public IMethodBuilderWithReturnValue<TReturnType> Method<TReturnType>(string methodName, params Type[] parameters)
        {
            return new MethodBuilderWithReturnValue<TReturnType, TInterface>(GetMethod(methodName, parameters), this);
        }

        public TInterface Finish()
        {
            var fieldTypes = TypeFields.Select(o => o.Key.FieldType).ToArray();
            var cons = Builder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, fieldTypes);
            var cil = cons.GetILGenerator();
            
            int i = 1;
            foreach (var field in TypeFields.Keys)
            {
                cil.Emit(OpCodes.Ldarg_0);
                cil.Emit(OpCodes.Ldarg, i++);
                cil.Emit(OpCodes.Stfld, field);
            }
            
            cil.Emit(OpCodes.Ret);

            return (TInterface)Activator.CreateInstance(Builder.CreateTypeInfo(), TypeFields.Values.ToArray());
        }

        internal FieldBuilder DefineField(string name, object value)
        {
            var field = Builder.DefineField(name, value.GetType(), FieldAttributes.InitOnly);
            TypeFields.Add(field, value);

            return field;
        }

        private MethodInfo GetMethod(string name, Type[] parameters)
        {
            var matching = Methods.Where(o => o.Name == name).ToArray();

            if (matching.Length == 0)
                throw new KeyNotFoundException($"Method with name {name} not found in type {typeof(TInterface)}");

            if (matching.Length > 0)
            {
                matching = matching
                    .Where(o => o.GetParameters().Select(i => i.ParameterType).SequenceEqual(parameters))
                    .ToArray();

                if (matching.Length == 0)
                    throw new KeyNotFoundException($"Method with name {name} and {parameters.Length} parameters not found in type {typeof(TInterface)}");
            }

            return matching.Single();
        }

        internal object CreateImplementation(TExecutor executor)
        {
            /* This is how the generated class would look:
             * class <>ImplProxyName
             * {
             *      private readonly TExecutor Executor;
             * 
             *      public <>ImplProxyName(object executor)
             *      {
             *          this.Executor = executor;
             *      }
             *      
             *      public virtual AnImplementedMethod(int arg1, string arg2, ...)
             *      {
             *          this.Executor.methodToCall(
             *              methodof(<>ImplProxyName.AnImplementedMethod),
             *              new object[] { arg1, arg2, ... });
             *      }
             * }
             * */

            var interfaceType = typeof(TInterface);
            string name = interfaceType.Name;

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = "ProxyAssembly" + name }, AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("ProxyModule" + name);
            var tb = mb.DefineType("<>Impl" + name, TypeAttributes.Class, interfaceType);
            var field = tb.DefineField("Executor", typeof(TExecutor), FieldAttributes.Private | FieldAttributes.InitOnly);

            //Override all interface methods
            foreach (var item in interfaceType.GetMethods())
            {
                var paramTypes = item.GetParameters().Select(o => o.ParameterType).ToArray();
                var method = tb.DefineMethod(item.Name, MethodAttributes.Public | MethodAttributes.Virtual, null, paramTypes);
                var il = method.GetILGenerator();

                //Loads the instance field in order to call its method
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);

                //Get the currently calling method
                il.EmitCall(OpCodes.Call, typeof(MethodBase).GetMethod(nameof(MethodBase.GetCurrentMethod)), null);

                //Creates a new array to contain all parameters
                il.Emit(OpCodes.Ldc_I4, paramTypes.Length);
                il.Emit(OpCodes.Newarr, typeof(object));

                for (int i = 0; i < paramTypes.Length; i++)
                {
                    //Loads item
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i); //Index of the array to set
                    il.Emit(OpCodes.Ldarg, i + 1); //Load parameter

                    //Boxes it if it's a value type
                    if (paramTypes[i].IsValueType)
                        il.Emit(OpCodes.Box, paramTypes[i]);

                    //Adds the item into the array
                    il.Emit(OpCodes.Stelem_Ref);
                }

                il.EmitCall(OpCodes.Callvirt, typeof(TExecutor).GetMethod("Invoke"), null);
                il.Emit(OpCodes.Ret);
            }

            var cons = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(TExecutor) });
            var cil = cons.GetILGenerator();

            //Creates a constructor that loads the first parameter and sets it to the instance field
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Ldarg_1);
            cil.Emit(OpCodes.Stfld, field);
            cil.Emit(OpCodes.Ret);

            return Activator.CreateInstance(tb.CreateTypeInfo(), executor);
        }
    }
}
