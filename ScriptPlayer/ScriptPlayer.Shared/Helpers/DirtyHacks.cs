using System;
using System.Reflection;

namespace ScriptPlayer.Shared
{
    public static class DirtyHacks
    {
        public static T GetPrivateField<T>(object obj, string fieldName)
        {
            return (T)obj?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        }

        public static T GetPublicProperty<T>(object obj, string propertyName)
        {
            return (T)obj?.GetType().GetProperty(propertyName)?.GetValue(obj);
        }

        public static T GetAnythingWithThatName<T>(object obj, string fieldOrProperty)
        {
            FieldInfo field = obj?.GetType().GetField(fieldOrProperty, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return (T)field.GetValue(obj);

            return (T)obj?.GetType().GetProperty(fieldOrProperty)?.GetValue(obj);
        }
    }
}
