using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
#if HDRP_AVAILABLE
using UnityEditor.Recorder.AOV;
#endif
using UnityEditor.Recorder.FrameCapturer;

namespace UnityEditor.Recorder
{
    class RecorderInfo
    {
        public Type recorderType;
        public Type settingsType;
        public string displayName;
        public string iconName;
        public bool deprecated;
    }

    static class RecordersInventory
    {
        static Dictionary<Type, RecorderInfo> s_Recorders;
        static HashSet<RecorderInfo> s_BuiltInRecorderInfos;
        static HashSet<RecorderInfo> s_LegacyRecorderInfos;

        static IEnumerable<KeyValuePair<Type, object[]>> FindRecorders()
        {
            var attribType = typeof(RecorderSettingsAttribute);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = a.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogWarning($"Failed to look for Recorder Settings in assembly '{a.FullName}': {e.Message}");
                    continue;
                }

                foreach (var t in types)
                {
                    var attributes = t.GetCustomAttributes(attribType, false);
                    if (attributes.Length != 0)
                        yield return new KeyValuePair<Type, object[]>(t, attributes);
                }
            }
        }

        static void Init()
        {
            if (s_Recorders == null)
            {
                s_Recorders = new Dictionary<Type, RecorderInfo>();
                foreach (var recorder in FindRecorders())
                {
                    var settingsType = recorder.Key;
                    var settingsAttribs = recorder.Value;

                    if (settingsType == null || string.IsNullOrEmpty(settingsType.FullName))
                        continue;

                    if (settingsAttribs.Length == 1)
                    {
                        var settingsAttrib = (RecorderSettingsAttribute)settingsAttribs[0];

                        var info = new RecorderInfo
                        {
                            settingsType = settingsType,
                            recorderType = settingsAttrib.recorderType,
                            displayName = settingsAttrib.displayName,
                            iconName = settingsAttrib.iconName,
                            deprecated = settingsAttrib.deprecated
                        };

                        s_Recorders.Add(settingsType, info);
                    }
                }
            }

            if (s_Recorders != null)
            {
                if (s_BuiltInRecorderInfos == null)
                {
                    s_BuiltInRecorderInfos = new HashSet<RecorderInfo>
                    {
                        s_Recorders[typeof(AnimationRecorderSettings)],
                        s_Recorders[typeof(MovieRecorderSettings)],
                        s_Recorders[typeof(ImageRecorderSettings)],
#if HDRP_AVAILABLE
                        s_Recorders[typeof(AOVRecorderSettings)],
#endif
                        s_Recorders[typeof(AudioRecorderSettings)]
                    };
                }

                if (s_LegacyRecorderInfos == null)
                {
                    s_LegacyRecorderInfos = new HashSet<RecorderInfo>
                    {
#pragma warning disable 618
                        s_Recorders[typeof(GIFRecorderSettings)],
#pragma warning restore 618
                        s_Recorders[typeof(MP4RecorderSettings)],
                        s_Recorders[typeof(EXRRecorderSettings)],
                        s_Recorders[typeof(PNGRecorderSettings)],
                        s_Recorders[typeof(WEBMRecorderSettings)]
                    };
                }
            }
        }

        internal static RecorderInfo GetRecorderInfo(Type settingsType)
        {
            Init();

            if (settingsType == null || string.IsNullOrEmpty(settingsType.FullName))
                return null;

            return s_Recorders.ContainsKey(settingsType) ? s_Recorders[settingsType] : null;
        }

        internal static IEnumerable<RecorderInfo> builtInRecorderInfos
        {
            get
            {
                Init();
                return s_BuiltInRecorderInfos;
            }
        }

        internal static IEnumerable<RecorderInfo> legacyRecorderInfos
        {
            get
            {
                Init();
                return s_LegacyRecorderInfos;
            }
        }

        internal static IEnumerable<RecorderInfo> customRecorderInfos
        {
            get
            {
                Init();
                var custom = s_Recorders.Values.Where(r => !s_BuiltInRecorderInfos.Contains(r) && !s_LegacyRecorderInfos.Contains(r));
#if HDRP_AVAILABLE
                return custom;
#else
                // Remove AOV Recorder from list of custom because it will be inserted in the above catch-all other logic
                var result = custom.Where(c => c.recorderType != typeof(AOVRecorder));
                return result;
#endif
            }
        }

        internal static Recorder CreateDefaultRecorder(RecorderSettings recorderSettings)
        {
            Init();
            var factory = GetRecorderInfo(recorderSettings.GetType());
            if (factory != null)
            {
                var recorder = (Recorder)ScriptableObject.CreateInstance(factory.recorderType);
                recorder.Reset();
                recorder.settings = recorderSettings;
                return recorder;
            }

            throw new ArgumentException("No factory was registered for " + recorderSettings.GetType().Name);
        }

        internal static RecorderSettings CreateDefaultRecorderSettings(Type settingsType)
        {
            Init();
            var recorderinfo = GetRecorderInfo(settingsType);
            if (recorderinfo != null)
            {
                var settings = (RecorderSettings)ObjectFactory.CreateInstance(recorderinfo.settingsType);
                settings.name = recorderinfo.displayName;

                return settings;
            }

            throw new ArgumentException("No factory was registered for " + settingsType.Name);
        }
    }
}
