using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Entities.Editor
{
    class LiveLinkToolbar
    {
        static readonly Action s_RepaintToolbar = BuildRepaintToolbarDelegate() ?? InternalEditorUtility.RepaintAllViews;

        internal static Action BuildRepaintToolbarDelegate()
        {
            var toolbarRepaintMethod = Type.GetType("UnityEditor.Toolbar, UnityEditor")?.GetMethod("RepaintToolbar", BindingFlags.NonPublic | BindingFlags.Static);
            if (toolbarRepaintMethod != null && toolbarRepaintMethod.GetParameters().Length == 0)
                return (Action)Delegate.CreateDelegate(typeof(Action), toolbarRepaintMethod);

            return null;
        }

        internal static void RepaintPlaybar() => s_RepaintToolbar();

        [CommandHandler("Test/Toolbar", CommandHint.UI)]
        static void DrawPlaybar(CommandExecuteContext ctx) {

            GUILayout.Button("TEST");
        }
    }
}
