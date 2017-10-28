using System.Reflection;

namespace ScriptPlayer.Shared
{
    public static class DirtyHacks
    {
        public static T GetPrivateField<T>(object obj, string fieldName)
        {
            return (T)obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }
    }
}
