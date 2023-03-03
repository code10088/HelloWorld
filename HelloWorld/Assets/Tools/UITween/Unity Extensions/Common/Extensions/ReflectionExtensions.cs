using System;
using System.Reflection;

namespace UnityExtensions
{
    /// <summary>
    /// Extensions for Reflection.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Get other type in the same assembly.
        /// </summary>
        public static Type GetOtherTypeInSameAssembly(this Type type, string otherTypeFullName)
        {
            var assemblyQualifiedName = type.AssemblyQualifiedName;
            otherTypeFullName += assemblyQualifiedName.Substring(assemblyQualifiedName.IndexOf(','));
            return Type.GetType(otherTypeFullName);
        }


        /// <summary>
        /// Find a field info (start from specified type, include all base types). 
        /// </summary>
        public static FieldInfo GetFieldUpwards(this Type type, string fieldName, BindingFlags flags)
        {
            flags |= BindingFlags.DeclaredOnly;
            while (type != null)
            {
                var info = type.GetField(fieldName, flags);
                if (info != null) return info;
                type = type.BaseType;
            }
            return null;
        }


        /// <summary>
        /// Find a property info (start from specified type, include all base types). 
        /// </summary>
        static PropertyInfo GetPropertyUpwards(this Type type, string propertyName, BindingFlags flags)
        {
            flags |= BindingFlags.DeclaredOnly;
            while (type != null)
            {
                var info = type.GetProperty(propertyName, flags);
                if (info != null) return info;
                type = type.BaseType;
            }
            return null;
        }


        /// <summary>
        /// Find a method info (start from specified type, include all base types). 
        /// </summary>
        static MethodInfo GetMethodUpwards(this Type type, string methodName, BindingFlags flags)
        {
            flags |= BindingFlags.DeclaredOnly;
            while (type != null)
            {
                var info = type.GetMethod(methodName, flags);
                if (info != null) return info;
                type = type.BaseType;
            }
            return null;
        }


        /// <summary>
        /// Find an instance field info.
        /// </summary>
        public static FieldInfo GetInstanceField(this Type type, string fieldName)
        {
            return type.GetFieldUpwards(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }


        /// <summary>
        /// Find an static field info.
        /// </summary>
        public static FieldInfo GetStaticField(this Type type, string fieldName)
        {
            return type.GetFieldUpwards(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }


        /// <summary>
        /// Find an instance property info.
        /// </summary>
        public static PropertyInfo GetInstanceProperty(this Type type, string propertyName)
        {
            return type.GetPropertyUpwards(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }


        /// <summary>
        /// Find an static property info.
        /// </summary>
        public static PropertyInfo GetStaticProperty(this Type type, string propertyName)
        {
            return type.GetPropertyUpwards(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }


        /// <summary>
        /// Find an instance method info.
        /// </summary>
        public static MethodInfo GetInstanceMethod(this Type type, string methodName)
        {
            return type.GetMethodUpwards(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }


        /// <summary>
        /// Find an static method info.
        /// </summary>
        public static MethodInfo GetStaticMethod(this Type type, string methodName)
        {
            return type.GetMethodUpwards(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }


        /// <summary>
        /// Set instance field value.
        /// </summary>
        public static void SetFieldValue(this object instance, string fieldName, object value)
        {
            instance.GetType().GetInstanceField(fieldName).SetValue(instance, value);
        }


        /// <summary>
        /// Get instance field value.
        /// </summary>
        public static object GetFieldValue(this object instance, string fieldName)
        {
            return instance.GetType().GetInstanceField(fieldName).GetValue(instance);
        }


        /// <summary>
        /// Set static field value.
        /// </summary>
        public static void SetFieldValue(this Type type, string fieldName, object value)
        {
            type.GetStaticField(fieldName).SetValue(null, value);
        }


        /// <summary>
        /// Get static field value.
        /// </summary>
        public static object GetFieldValue(this Type type, string fieldName)
        {
            return type.GetStaticField(fieldName).GetValue(null);
        }


        /// <summary>
        /// Set instance property value.
        /// </summary>
        public static void SetPropertyValue(this object instance, string propertyName, object value)
        {
            instance.GetType().GetInstanceProperty(propertyName).SetValue(instance, value);
        }


        /// <summary>
        /// Get instance property value.
        /// </summary>
        public static object GetPropertyValue(this object instance, string propertyName)
        {
            return instance.GetType().GetInstanceProperty(propertyName).GetValue(instance);
        }


        /// <summary>
        /// Set static property value.
        /// </summary>
        public static void SetPropertyValue(this Type type, string propertyName, object value)
        {
            type.GetStaticProperty(propertyName).SetValue(null, value);
        }


        /// <summary>
        /// Get static property value.
        /// </summary>
        public static object GetPropertyValue(this Type type, string propertyName)
        {
            return type.GetStaticProperty(propertyName).GetValue(null);
        }


        /// <summary>
        /// Invoke an instance method.
        /// </summary>
        public static object InvokeMethod(this object instance, string methodName, params object[] parameters)
        {
            return instance.GetType().GetInstanceMethod(methodName).Invoke(instance, parameters);
        }


        /// <summary>
        /// Invoke an static method.
        /// </summary>
        public static object InvokeMethod(this Type type, string methodName, params object[] parameters)
        {
            return type.GetStaticMethod(methodName).Invoke(null, parameters);
        }

    } // class Extensions

} // namespace UnityExtensions