using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Recorder.Editor.Tests")]

namespace UnityEditor.Recorder.Input
{
    static class GameViewSize
    {
        static object s_InitialSizeObj;
        public static int modifiedResolutionCount;
        const int miscSize = 1; // Used when no main GameView exists (ex: batchmode)

        static Type s_GameViewType = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
        static string s_GetGameViewFuncName = "GetMainPlayModeView";
        static EditorWindow GetMainGameView()
        {
            var getMainGameView = s_GameViewType.GetMethod(s_GetGameViewFuncName, BindingFlags.NonPublic | BindingFlags.Static);
            if (getMainGameView == null)
            {
                Debug.LogError(string.Format("Can't find the main Game View : {0} function was not found in {1} type ! Did API change ?",
                    s_GetGameViewFuncName, s_GameViewType));
                return null;
            }
            var res = getMainGameView.Invoke(null, null);
            return (EditorWindow)res;
        }

        public static void DisableMaxOnPlay()
        {
            var gameView = GetMainGameView();
            if (gameView == null)
                return;

            var getMaximizeOnPlayMethod = gameView.GetType().GetMethod("get_maximizeOnPlay",  BindingFlags.Public | BindingFlags.Instance);

            bool maximizeOnPlay = false;
            if (getMaximizeOnPlayMethod != null)
            {
                maximizeOnPlay = (bool)getMaximizeOnPlayMethod.Invoke(gameView, new object[] {});
                if (maximizeOnPlay)
                {
                    Debug.LogWarning("'Maximize on Play' not compatible with recorder: disabling it!");
                    var m = gameView.GetType().GetMethod("set_maximizeOnPlay",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    m.Invoke(gameView, new object[] {false});
                }
            }
        }

        public static void GetGameRenderSize(out int width, out int height)
        {
            var gameView = GetMainGameView();

            if (gameView == null)
            {
                width = height = miscSize;
                return;
            }

            var prop = gameView.GetType().GetProperty("targetSize", BindingFlags.NonPublic | BindingFlags.Instance);
            var size = (Vector2)prop.GetValue(gameView, new object[] {});
            width = (int)size.x;
            height = (int)size.y;
        }

        static object Group()
        {
            var T = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var sizes = T.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = sizes.GetValue(null, new object[] {});

            var currentGroup = instance.GetType().GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);
            var group = currentGroup.GetValue(instance, new object[] {});
            return group;
        }

        public static object SetCustomSize(int width, int height)
        {
            var sizeObj = FindRecorderSizeObj();
            if (sizeObj != null)
            {
                sizeObj.GetType().GetField("m_Width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sizeObj, width);
                sizeObj.GetType().GetField("m_Height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sizeObj, height);
            }
            else
            {
                sizeObj = AddSize(width, height);
            }

            return sizeObj;
        }

        static object FindRecorderSizeObj()
        {
            var group = Group();

            var customs = group.GetType().GetField("m_Custom", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

            var itr = (IEnumerator)customs.GetType().GetMethod("GetEnumerator").Invoke(customs, new object[] {});
            while (itr.MoveNext())
            {
                var txt = (string)itr.Current.GetType().GetField("m_BaseText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itr.Current);
                if (txt == "(Recording resolution)")
                    return itr.Current;
            }

            return null;
        }

        static int IndexOf(object sizeObj)
        {
            var group = Group();
            var method = group.GetType().GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance);
            var index = (int)method.Invoke(group, new[] {sizeObj});

            var builtinList = group.GetType().GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

            method = builtinList.GetType().GetMethod("Contains");
            if ((bool)method.Invoke(builtinList, new[] { sizeObj }))
                return index;

            method = group.GetType().GetMethod("GetBuiltinCount");
            index += (int)method.Invoke(group, new object[] {});

            return index;
        }

        static object NewSizeObj(int width, int height)
        {
            var T = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var tt = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            var c = T.GetConstructor(new[] {tt, typeof(int), typeof(int), typeof(string)});
            var sizeObj = c.Invoke(new object[] {1, width, height,  "(Recording resolution)"});
            return sizeObj;
        }

        public static object AddSize(int width, int height)
        {
            var sizeObj = NewSizeObj(width, height);

            var group = Group();
            var obj = group.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(group, new[] {sizeObj});

            return sizeObj;
        }

        public static void SelectSize(object size)
        {
            if (size == null)
                return;
            var index = IndexOf(size);

            var gameView = GetMainGameView();
            if (gameView == null)
                return;
            var obj = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(gameView, new[] { index, size });
        }

        public static object currentSize
        {
            get
            {
                var gv = GetMainGameView();
                if (gv == null)
                    return new[] {miscSize, miscSize};
                var prop = gv.GetType().GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                return prop.GetValue(gv, new object[] {});
            }
        }

        public static void BackupCurrentSize()
        {
            s_InitialSizeObj = currentSize;
        }

        public static void RestoreSize()
        {
            SelectSize(s_InitialSizeObj);
            s_InitialSizeObj = null;
        }
    }
}
