using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ClassImpl
{
    public static class ClassUtils
    {
        private static readonly IDictionary<(Type Type, bool SetData), Func<object, object, object>> CopyFuncCache = new Dictionary<(Type, bool), Func<object, object, object>>();

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
            var field = type.GetField(Implementer.CustomDataField, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException("The object must have been returned by Implementer.Finish()", nameof(type));

            return field;
        }

        private static T Copy<T>(T obj, object newData, bool setNewData)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var t = obj.GetType();

            if (!CopyFuncCache.TryGetValue((t, setNewData), out var val))
            {
                var dataParam = Expression.Parameter(typeof(object), "data");
                var objParam = Expression.Parameter(typeof(object), "obj");

                var body = new List<Expression>();

                var objFields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(o => o.Name.StartsWith("<>")).ToArray();
                var fieldValueExprs = new List<Expression>();

                foreach (var field in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(o => o.Name.StartsWith("<>")))
                {
                    fieldValueExprs.Add(Expression.MakeMemberAccess(Expression.Convert(objParam, t), field));
                }

                if (setNewData)
                {
                    fieldValueExprs[0] = Expression.Convert(dataParam, objFields[0].FieldType);
                }

                body.Add(Expression.New(t.GetConstructors()[0], fieldValueExprs));

                CopyFuncCache[(t, setNewData)] = val = Expression.Lambda<Func<object, object, object>>(
                    Expression.Block(body),
                    objParam, dataParam).Compile();
            }

            return (T)val(obj, newData);
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
}
