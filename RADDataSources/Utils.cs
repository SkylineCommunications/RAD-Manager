namespace RadDataSources
{
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Analytics.DataTypes;
    using Skyline.DataMiner.Net;

    public static class Utils
    {
        public static ParamID ToParamID(this ParameterKey key)
        {
            if (key == null)
            {
                return null;
            }

            return new ParamID(key.DataMinerID, key.ElementID, key.ParameterID, key.Instance);
        }

        public static string ParameterKeyToString(ParameterKey pKey)
        {
            if (pKey == null)
                return string.Empty;

            string result = $"{pKey.DataMinerID}/{pKey.ElementID}/{pKey.ParameterID}";
            string instance = !string.IsNullOrEmpty(pKey.DisplayInstance) ? pKey.DisplayInstance : pKey.Instance;
            return string.IsNullOrEmpty(instance) ? result : $"{result}/{instance}";
        }

        public static string ParameterKeysToString(IEnumerable<ParameterKey> pKeys)
        {
            if (pKeys == null)
                return string.Empty;

            return $"[{string.Join(", ", pKeys.Select(p => ParameterKeyToString(p)))}]";
        }
    }
}
