﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ClassImpl
{
    /// <summary>
    /// Base non-generic implementer.
    /// </summary>
    public class Implementer
    {
        protected readonly Type Type;

        /// <summary>
        /// Implementable properties of type.
        /// </summary>
        public PropertyInfo[] Properties { get; }

        /// <summary>
        /// Implementable methods of type.
        /// </summary>
        public MethodInfo[] Methods { get; }

        internal readonly TypeBuilder Builder;
        internal readonly IDictionary<FieldBuilder, object> TypeFields = new Dictionary<FieldBuilder, object>();

        private bool IsFinished;

        /// <summary>
        /// Instantiates an implementer that implements the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to implement.</param>
        public Implementer(Type type)
        {
            Type = type;
            Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Methods = Type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            string name = Type.Name;

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = "ProxyAssembly" + name }, AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("ProxyModule" + name);

            if (Type.IsInterface)
                Builder = mb.DefineType("<>Impl" + name, TypeAttributes.Class, null, new[] { Type });
            else
                Builder = mb.DefineType("<>Impl" + name, TypeAttributes.Class, Type);
        }

        /// <summary>
        /// Finishes implementing the type and returns the implemented instance. Make sure to only call this method once.
        /// </summary>
        public object Finish()
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

            return Activator.CreateInstance(Builder.CreateTypeInfo(), TypeFields.Values.ToArray());
        }

        /// <summary>
        /// Adds a catch-all handler.
        /// </summary>
        /// <param name="handler">The action to invoke when any method gets called on the type.
        /// This method receives the method that was called and its arguments.</param>
        public void HandleAll(Action<MethodBase, IDictionary<string, object>> handler)
        {
            foreach (var item in Methods.Where(o => o.ReturnType == typeof(void)))
            {
                new MethodBuilder(Type, item, this).Callback(o => handler(item, o));
            }
        }

        /// <summary>
        /// Start implementing a method.
        /// </summary>
        /// <param name="method">The method to implement.</param>
        public IMethodBuilder Member(MethodInfo method)
        {
            return new MethodBuilder(Type, method, this);
        }

        /// <summary>
        /// Starts implementing a property getter.
        /// </summary>
        /// <param name="prop">The property whose getter to implement.</param>
        public IMethodBuilderWithReturnValue<object> Getter(PropertyInfo prop)
        {
            return new MethodBuilderWithReturnValue<object>(Type, prop.GetMethod, this);
        }

        /// <summary>
        /// Starts implementing a property setter.
        /// </summary>
        /// <param name="prop">The property whose setter to implement.</param>
        public void Setter(PropertyInfo prop, Action<object> setter)
        {
            var builder = new MethodBuilder(Type, prop.SetMethod, this);
            builder.Callback(o => setter(o.Values.First()));
        }

        internal FieldBuilder DefineField(string name, object value)
        {
            var field = Builder.DefineField(name, value.GetType(), FieldAttributes.InitOnly);
            TypeFields.Add(field, value);

            return field;
        }
    }

    /// <summary>
    /// Generic implementer.
    /// </summary>
    public class Implementer<TInterface> : Implementer
    {
        public Implementer() : base(typeof(TInterface))
        {
        }

        /// <summary>
        /// Returns a method builder, representing a method identified by <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression that calls the target method.</param>
        public IMethodBuilder Member(Expression<Action<TInterface>> expression)
        {
            if (expression.Body is MethodCallExpression call)
                return new MethodBuilder(Type, call.Method, this);

            throw new Exception("Expression must be a method call");
        }

        /// <summary>
        /// Returns a method builder representing a method or a property getter that returns a value of type
        /// <typeparamref name="TReturnType"/>, identified by <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TReturnType">The type of the value that the method returns.</typeparam>
        /// <param name="expression">An expression that calls the target method.</param>
        public IMethodBuilderWithReturnValue<TReturnType> Member<TReturnType>(Expression<Func<TInterface, TReturnType>> expression)
        {
            if (expression.Body is MethodCallExpression call)
                return new MethodBuilderWithReturnValue<TReturnType>(Type, call.Method, this);

            if (expression.Body is MemberExpression member && member.Member is PropertyInfo prop)
                return new MethodBuilderWithReturnValue<TReturnType>(Type, prop.GetAccessors()[0], this);

            throw new Exception("Expression must be a method call or a property access");
        }

        /// <summary>
        /// Adds a callback <paramref name="setter"/> to the property of type <typeparamref name="TProperty"/>
        /// identified by <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TProperty">The property's type.</typeparam>
        /// <param name="expression">An expression that references the target property.</param>
        public void Setter<TProperty>(Expression<Func<TInterface, TProperty>> expression, Action<TProperty> setter)
        {
            if (expression.Body is MemberExpression member && member.Member is PropertyInfo prop)
            {
                if (!prop.CanWrite)
                    throw new InvalidOperationException("Property is read-only");

                Setter(prop, o => setter((TProperty)o));
            }
            else
            {
                throw new Exception("Expression must be a property accessor");
            }
        }

        /// <summary>
        /// Finishes implementing the type and returns the implemented instance. Make sure to only call this method once.
        /// </summary>
        public new TInterface Finish()
        {
            return (TInterface)base.Finish();
        }
    }
}
