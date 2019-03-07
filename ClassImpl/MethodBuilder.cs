using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ClassImpl
{
    public delegate void MethodCallbackNoParams();
    public delegate void MethodCallbackWithParams(IDictionary<string, object> parameters);
    public delegate T MethodCallbackNoParamsReturns<T>();
    public delegate T MethodCallbackWithParamsReturns<T>(IDictionary<string, object> parameters);

    public interface IMethodBuilder
    {
        IMethodBuilder Callback(MethodCallbackNoParams action);
        IMethodBuilder Callback(MethodCallbackWithParams action);
    }

    internal class MethodBuilder : IMethodBuilder
    {
        private readonly Type Type;
        private readonly MethodInfo Method;
        private readonly Implementer Implementer;

        public MethodBuilder(Type type, MethodInfo method, Implementer implementer)
        {
            if (method.ReturnType != typeof(void))
                throw new Exception($"Expected a method that returns void, instead it returns {method.ReturnType}");

            this.Type = type;
            this.Method = method;
            this.Implementer = implementer;
        }

        public IMethodBuilder Callback(MethodCallbackNoParams action)
        {
            string name = $"{Type.Name}.{Method.Name}";
            var field = Implementer.DefineField(name + "Callback", action);

            var method = Implementer.Builder.DefineMethod(name, MethodAttributes.Private | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.EmitCall(OpCodes.Callvirt, action.GetType().GetMethod("Invoke"), null);
            il.Emit(OpCodes.Ret);

            Implementer.Builder.DefineMethodOverride(method, this.Method);

            return this;
        }

        public IMethodBuilder Callback(MethodCallbackWithParams action)
        {
            string name = $"{Type.Name}.{Method.Name}";
            var field = Implementer.DefineField(name + "Callback", action);

            var method = Implementer.Builder.DefineMethod(
                name: name,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                returnType: typeof(void),
                parameterTypes: Method.GetParameters().Select(o => o.ParameterType).ToArray());

            var il = method.GetILGenerator();

            il.CreateDicAndAddValues(Method);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, action.GetType().GetMethod("Invoke"), null);
            il.Emit(OpCodes.Ret);

            Implementer.Builder.DefineMethodOverride(method, this.Method);

            return this;
        }
    }

    public interface IMethodBuilderWithReturnValue<TReturned>
    {
        IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackNoParamsReturns<TReturned> func);
        IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackWithParamsReturns<TReturned> func);

        void Returns(TReturned value);
    }

    internal class MethodBuilderWithReturnValue<TReturned> : IMethodBuilderWithReturnValue<TReturned>
    {
        private readonly Type Type;
        private readonly MethodInfo Method;
        private readonly Implementer Implementer;

        public MethodBuilderWithReturnValue(Type type, MethodInfo method, Implementer implementer)
        {
            if (method.ReturnType != typeof(TReturned) && typeof(TReturned) != typeof(object))
                throw new Exception($"Expected a method that returns type {typeof(TReturned)}, instead it returns {method.ReturnType}");

            this.Type = type;
            this.Method = method;
            this.Implementer = implementer;
        }
        
        public IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackNoParamsReturns<TReturned> func)
        {
            string name = $"{Type.Name}.{Method.Name}";
            var field = Implementer.DefineField(name + "Callback", func);

            var method = Implementer.Builder.DefineMethod(
                name: name,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                returnType: Method.ReturnType,
                parameterTypes: null);

            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.EmitCall(OpCodes.Callvirt, func.GetType().GetMethod("Invoke"), null);

            if (Method.ReturnType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, Method.ReturnType);

            il.Emit(OpCodes.Ret);

            Implementer.Builder.DefineMethodOverride(method, this.Method);

            return this;
        }

        public IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackWithParamsReturns<TReturned> func)
        {
            string name = $"{Type.Name}.{Method.Name}";
            var field = Implementer.DefineField(name + "Callback", func);

            var method = Implementer.Builder.DefineMethod(
                name: name,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                returnType: Method.ReturnType,
                parameterTypes: Method.GetParameters().Select(o => o.ParameterType).ToArray());

            var il = method.GetILGenerator();
            il.CreateDicAndAddValues(Method);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, func.GetType().GetMethod("Invoke"), null);
            il.Emit(OpCodes.Ret);

            Implementer.Builder.DefineMethodOverride(method, this.Method);

            return this;
        }

        public void Returns(TReturned value) => Callback(() => value);
    }

    //public interface IMethodBuilderWithReturnValue
    //{
    //    IMethodBuilderWithReturnValue Callback(MethodCallbackNoParamsReturns<object> func);
    //    IMethodBuilderWithReturnValue Callback(MethodCallbackWithParamsReturns<object> func);

    //    void Returns(object value);
    //}

    //public class MethodBuilderWithReturnValue : IMethodBuilderWithReturnValue
    //{
    //    private IMethodBuilderWithReturnValue<object> Builder;

    //    public MethodBuilderWithReturnValue(Type type, MethodInfo method, Implementer implementer)
    //    {
    //        this.Builder = new MethodBuilderWithReturnValue<object>(type, method, implementer);
    //    }

    //    public IMethodBuilderWithReturnValue Callback(MethodCallbackNoParamsReturns<object> func)
    //    {

    //    }

    //    public IMethodBuilderWithReturnValue Callback(MethodCallbackWithParamsReturns<object> func)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Returns(object value)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
