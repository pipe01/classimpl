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

        private bool IsFinished;

        public Implementer()
        {
            var interfaceType = typeof(TInterface);
            
            Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Methods = Type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            string name = interfaceType.Name;

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = "ProxyAssembly" + name }, AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("ProxyModule" + name);

            if (interfaceType.IsInterface)
                Builder = mb.DefineType("<>Impl" + name, TypeAttributes.Class, null, new[] { interfaceType });
            else
                Builder = mb.DefineType("<>Impl" + name, TypeAttributes.Class, interfaceType);
        }

        /// <summary>
        /// Returns a method builder, representing a method identified by <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression that calls the target method.</param>
        public IMethodBuilder Member(Expression<Action<TInterface>> expression)
        {
            if (expression.Body is MethodCallExpression call)
                return new MethodBuilder<TInterface>(call.Method, this);
            
            throw new Exception("Expression must be a method call");
        }

        /// <summary>
        /// Returns a method builder, representing a method or a property that returns a value of type
        /// <typeparamref name="TReturnType"/> identified by <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TReturnType">The type of the value that the method returns.</typeparam>
        /// <param name="expression">An expression that calls the target method.</param>
        public IMethodBuilderWithReturnValue<TReturnType> Member<TReturnType>(Expression<Func<TInterface, TReturnType>> expression)
        {
            if (expression.Body is MethodCallExpression call)
                return new MethodBuilderWithReturnValue<TReturnType, TInterface>(call.Method, this);

            if (expression.Body is MemberExpression member && member.Member is PropertyInfo prop)
                return new MethodBuilderWithReturnValue<TReturnType, TInterface>(prop.GetAccessors()[0], this);

            throw new Exception("Expression must be a method call or a property access");
        }
        
        public void Setter<TProperty>(Expression<Func<TInterface, TProperty>> expression, Action<TProperty> setter)
        {
            if (expression.Body is MemberExpression member && member.Member is PropertyInfo prop)
            {
                if (!prop.CanWrite)
                    throw new InvalidOperationException("Property is read-only");

                var builder = new MethodBuilder<TInterface>(prop.GetAccessors()[1], this);
                builder.Callback(o => setter((TProperty)o.Values.First()));
            }
            else
            {
                throw new Exception("Expression must be a property accessor");
            }
        }

        public TInterface Finish()
        {
            if (IsFinished)
                throw new InvalidOperationException("This implementer has already been finished");

            IsFinished = true;

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
