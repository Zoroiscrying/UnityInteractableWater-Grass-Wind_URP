namespace UnityEditor.Recorder
{
    [CustomPropertyDrawer(typeof(AccumulationSettings.ShutterProfileType))]
    class ShutterProfileTypePropertyDrawer : EnumProperyDrawer<AccumulationSettings.ShutterProfileType>
    {
        protected override string ToLabel(AccumulationSettings.ShutterProfileType value)
        {
            switch (value)
            {
                case AccumulationSettings.ShutterProfileType.Curve:
                    return "Curve";

                case AccumulationSettings.ShutterProfileType.Range:
                    return "Range";

                default:
                    return "unknown";
            }
        }
    }
}
