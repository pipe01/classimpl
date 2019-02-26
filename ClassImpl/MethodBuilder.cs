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
            string name = $"{typeof(TInterface).Name}.{Method.Name}";
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
            string name = $"{typeof(TInterface).Name}.{Method.Name}";
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
            il.Emit(OpCodes.Ret);

            Implementer.Builder.DefineMethodOverride(method, this.Method);

            return this;
        }

        public IMethodBuilderWithReturnValue<TReturned> Callback(MethodCallbackWithParamsReturns<TReturned> func)
        {
            string name = $"{typeof(TInterface).Name}.{Method.Name}";
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
}
