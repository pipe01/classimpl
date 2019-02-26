using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

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
        /// Returns a method builder, representing a method identified by <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression that calls the target method.</param>
        public IMethodBuilder Method(Expression<Action<TInterface>> expression)
        {
            if (!(expression.Body is MethodCallExpression call))
                throw new Exception("Expression must be a method call");

            return new MethodBuilder<TInterface>(call.Method, this);
        }

        /// <summary>
        /// Returns a method builder, representing a method that returns a value of type <typeparamref name="TReturnType"/>
        /// identified by <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TReturnType">The type of the value that the method returns.</typeparam>
        /// <param name="expression">An expression that calls the target method.</param>
        public IMethodBuilderWithReturnValue<TReturnType> Method<TReturnType>(Expression<Func<TInterface, TReturnType>> expression)
        {
            if (!(expression.Body is MethodCallExpression call))
                throw new Exception("Expression must be a method call");

            return new MethodBuilderWithReturnValue<TReturnType, TInterface>(call.Method, this);
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
    }
}
