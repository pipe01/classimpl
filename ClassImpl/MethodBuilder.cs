using System;
using System.Collections.Generic;
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

    internal class MethodBuilder<TInterface> : IMethodBuilder
    {
        private readonly MethodInfo Method;
        private readonly Implementer<TInterface> Implementer;

        public MethodBuilder(MethodInfo method, Implementer<TInterface> implementer)
        {
            if (method.ReturnType != typeof(void))
                throw new Exception($"Expected a method that returns void, instead it returns {method.ReturnType}");

            this.Method = method;
            this.Implementer = implementer;
        }

        public IMethodBuilder Callback(MethodCallbackNoParams action)
        {
            string name = $"{typeof(TInterface).Name}.{Method.Name}";
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
            throw new NotImplementedException();
        }
    }

    public interface IMethodBuilderWithReturnValue<TReturned>
    {
        IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackNoParamsReturns<TReturned> func);
        IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackWithParamsReturns<TReturned> func);
    }

    public class MethodBuilderWithReturnValue<TReturned, TInterface> : IMethodBuilderWithReturnValue<TReturned>
    {
        private readonly MethodInfo Method;
        private readonly Implementer<TInterface> Implementer;

        public MethodBuilderWithReturnValue(MethodInfo method, Implementer<TInterface> implementer)
        {
            if (method.ReturnType != typeof(TReturned))
                throw new Exception($"Expected a method that returns type {typeof(TReturned)}, instead it returns {method.ReturnType}");

            this.Method = method;
            this.Implementer = implementer;
        }


        public IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackNoParamsReturns<TReturned> func)
        {
            throw new NotImplementedException();
        }

        public IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackWithParamsReturns<TReturned> func)
        {
            throw new NotImplementedException();
        }
    }
}
