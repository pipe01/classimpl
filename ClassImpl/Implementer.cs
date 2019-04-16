using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ClassImpl
{
    /// <summary>
    /// Represents a setter callback.
    /// </summary>
    /// <param name="value">The value that the property wants to be set to.</param>
    public delegate void SetterDelegate(object value);

    /// <summary>
    /// Represents a setter callback with custom data.
    /// </summary>
    /// <param name="value">The value that the property wants to be set to.</param>
    /// <param name="customData">The custom data for the object.</param>
    public delegate void SetterDelegateWithData(object value, object customData);

    /// <summary>
    /// Represents a setter callback.
    /// </summary>
    /// <param name="value">The value that the property wants to be set to.</param>
    /// <typeparam name="T">The type of the property's value.</typeparam>
    public delegate void SetterDelegate<T>(T value);

    /// <summary>
    /// Represents a setter callback with custom data.
    /// </summary>
    /// <param name="value">The value that the property wants to be set to.</param>
    /// <param name="customData">The custom data for the object.</param>
    /// <typeparam name="T">The type of the property's value.</typeparam>
    public delegate void SetterDelegateWithData<T>(T value, object customData);

    /// <summary>
    /// Base non-generic implementer.
    /// </summary>
    public class Implementer
    {
        internal const string CustomDataField = "<>CustomData";

        protected readonly Type Type;

        /// <summary>
        /// Implementable properties of type.
        /// </summary>
        public PropertyInfo[] Properties { get; }

        /// <summary>
        /// Implementable methods of type.
        /// </summary>
        public MethodInfo[] Methods { get; }

        /// <summary>
        /// The custom data type.
        /// </summary>
        public Type DataType { get; }

        internal readonly TypeBuilder Builder;
        internal readonly IDictionary<FieldInfo, object> TypeFields = new Dictionary<FieldInfo, object>();

        private bool IsFinished;

        internal readonly FieldBuilder DataField;

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
        /// Instantiates an implementer that implements the specified <paramref name="type"/> with arbitrary
        /// custom data.
        /// </summary>
        /// <param name="type">The type to implement.</param>
        /// <param name="dataType">The type of the custom data to use. This data can be referenced on all callbacks
        /// as a param with name '__data'.</param>
        public Implementer(Type type, Type dataType) : this(type)
        {
            this.DataType = dataType;

            this.DataField = Builder.DefineField(CustomDataField, DataType, FieldAttributes.Private);
        }

        /// <summary>
        /// Finishes implementing the type and returns the implemented instance. Make sure to only call this method once.
        /// </summary>
        /// <param name="data">The data to pass to all callbacks.</param>
        public object Finish(object data = null)
        {
            if (IsFinished)
                throw new InvalidOperationException("This implementer has already been finished");

            IsFinished = true;

            var fieldTypes = TypeFields.Select(o => o.Key.FieldType).ToArray();

            var ctorParams = DataType == null ? fieldTypes : new[] { DataType }.Concat(fieldTypes).ToArray();
            var ctor = Builder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, ctorParams);

            var cil = ctor.GetILGenerator();

            if (DataType != null)
            {
                cil.Emit(OpCodes.Ldarg_0);
                cil.Emit(OpCodes.Ldarg_1);
                cil.Emit(OpCodes.Stfld, DataField);
            }

            int i = 1 + (DataType != null ? 1 : 0);
            foreach (var field in TypeFields.Keys)
            {
                cil.Emit(OpCodes.Ldarg_0);
                cil.Emit(OpCodes.Ldarg, i++);
                cil.Emit(OpCodes.Stfld, field);
            }

            cil.Emit(OpCodes.Ret);

            return Activator.CreateInstance(Builder.CreateTypeInfo(),
                (DataType != null ? new[] { data } : new object[0]).Concat(TypeFields.Values).ToArray());
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
        /// Implements a property setter with custom data.
        /// </summary>
        /// <param name="prop">The property whose setter to implement.</param>
        public void Setter(PropertyInfo prop, SetterDelegateWithData setter)
        {
            if (DataType == null)
                throw new InvalidOperationException("This object doesn't have any custom data!");

            var builder = new MethodBuilder(Type, prop.SetMethod, this);
            builder.Callback(o => setter(o["value"], o.TryGetValue("__data", out var d) ? d : null));
        }

        /// <summary>
        /// Implements a property setter without custom data.
        /// </summary>
        /// <param name="prop">The property whose setter to implement.</param>
        public void Setter(PropertyInfo prop, SetterDelegate setter)
        {
            var builder = new MethodBuilder(Type, prop.SetMethod, this);
            builder.Callback(o => setter(o["value"]));
        }

        internal FieldBuilder DefineField(string name, object value)
        {
            var field = Builder.DefineField("<>" + name, value.GetType(), FieldAttributes.InitOnly);
            TypeFields.Add(field, value);

            return field;
        }

        /// <summary>
        /// Sets the custom data for an implemented object that has been returned by <see cref="Finish(object)"/>
        /// or <see cref="Implementer{TInterface}.Finish(object)"/>.
        /// </summary>
        /// <param name="implementedObject">The implemented object.</param>
        /// <param name="data">The data to be set.</param>
        public static void SetData(object implementedObject, object data)
        {
            GetDataField(implementedObject?.GetType() ?? throw new ArgumentNullException(nameof(implementedObject)))
                    .SetValue(implementedObject, data);
        }

        /// <summary>
        /// Gets the custom data for an implemented object that has been returned by <see cref="Finish(object)"/>
        /// or <see cref="Implementer{TInterface}.Finish(object)"/>.
        /// </summary>
        /// <param name="implementedObject">The implemented object.</param>
        public static object GetData(object implementedObject)
        {
            return GetDataField(implementedObject?.GetType() ?? throw new ArgumentNullException(nameof(implementedObject)))
                    .GetValue(implementedObject);
        }

        private static FieldInfo GetDataField(Type type)
        {
            var field = type.GetField(CustomDataField, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException("The object must have been returned by Implementer.Finish()", nameof(type));

            return field;
        }

        private static T Copy<T>(T obj, object newData, bool setNewData)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var t = obj.GetType();

            if (!t.Name.StartsWith("<>Impl"))
                throw new ArgumentException("Invalid object");

            var fieldValues = new List<object>();

            foreach (var item in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(o => o.Name.StartsWith("<>")))
            {
                fieldValues.Add(item.GetValue(obj));
            }

            if (setNewData)
            {
                if (t.GetField("<>CustomData", BindingFlags.NonPublic | BindingFlags.Instance) == null)
                    throw new InvalidOperationException("Tried to set custom data on an object that doesn't have any");

                fieldValues[0] = newData;
            }

            return (T)Activator.CreateInstance(t, fieldValues.ToArray());
        }

        /// <summary>
        /// Copies an implemented object with new custom data. This is orders of magnitude faster than re-implementing it.
        /// </summary>
        /// <typeparam name="T">The type of the implemented interface.</typeparam>
        /// <param name="obj">The implemented object to copy.</param>
        /// <param name="newData">The new data to set on the copied object.</param>
        public static T Copy<T>(T obj, object newData) => Copy(obj, newData, true);

        /// <summary>
        /// Copies an implemented object. This is orders of magnitude faster than re-implementing it.
        /// </summary>
        /// <typeparam name="T">The type of the implemented interface.</typeparam>
        /// <param name="obj">The implemented object to copy.</param>
        public static T Copy<T>(T obj) => Copy(obj, null, false);
    }

    /// <summary>
    /// Generic implementer.
    /// </summary>
    public class Implementer<TInterface> : Implementer
    {
        /// <summary>
        /// Instantiates an implementer that implements the specified <typeparamref name="TInterface"/>.
        /// </summary>
        public Implementer() : base(typeof(TInterface))
        {
        }

        /// <summary>
        /// Instantiates an implementer that implements the specified <typeparamref name="TInterface"/> with arbitrary
        /// custom data.
        /// </summary>
        /// <param name="dataType">The type of the custom data to use. This data can be referenced on all callbacks
        /// as a param with name '__data'.</param>
        public Implementer(Type dataType) : base(typeof(TInterface), dataType)
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
        /// Adds a callback <paramref name="setter"/> that accepts custom data to the property of type <typeparamref name="TProperty"/>
        /// identified by <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TProperty">The property's type.</typeparam>
        /// <param name="expression">An expression that references the target property.</param>
        public void Setter<TProperty>(Expression<Func<TInterface, TProperty>> expression, SetterDelegateWithData<TProperty> setter)
        {
            if (DataType == null)
                throw new InvalidOperationException("This object doesn't have any custom data!");

            if (expression.Body is MemberExpression member && member.Member is PropertyInfo prop)
            {
                if (!prop.CanWrite)
                    throw new InvalidOperationException("Property is read-only");

                Setter(prop, (value, customData) => setter((TProperty)value, customData));
            }
            else
            {
                throw new Exception("Expression must be a property accessor");
            }
        }

        /// <summary>
        /// Adds a callback <paramref name="setter"/> to the property of type <typeparamref name="TProperty"/>
        /// identified by <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TProperty">The property's type.</typeparam>
        /// <param name="expression">An expression that references the target property.</param>
        public void Setter<TProperty>(Expression<Func<TInterface, TProperty>> expression, SetterDelegate<TProperty> setter)
        {
            if (expression.Body is MemberExpression member && member.Member is PropertyInfo prop)
            {
                if (!prop.CanWrite)
                    throw new InvalidOperationException("Property is read-only");

                Setter(prop, value => setter((TProperty)value));
            }
            else
            {
                throw new Exception("Expression must be a property accessor");
            }
        }

        /// <summary>
        /// Finishes implementing the type and returns the implemented instance. Make sure to only call this method once.
        /// </summary>
        /// <param name="data">The data to pass to all callbacks.</param>
        public new TInterface Finish(object data = null)
        {
            return (TInterface)base.Finish(data);
        }
    }
}
