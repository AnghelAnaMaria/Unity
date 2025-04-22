#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

public static class ConsoleUtil
{
    public static void ClearConsole()
    {
#if UNITY_EDITOR
        // Obține tipul pentru clasa internă "UnityEditor.LogEntries"
        System.Type logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        if (logEntries != null)
        {
            // Obține metoda Clear() din LogEntries
            MethodInfo clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            if (clearMethod != null)
            {
                clearMethod.Invoke(null, null);
            }
        }
#endif
    }
}
