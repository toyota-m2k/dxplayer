using System;

namespace dxplayer.misc
{
    public static class Utils {
        public static T ParseToEnum<T>(string name, T defValue, bool igonreCase=true) where T : struct {
            if (Enum.TryParse<T>(name, igonreCase, out T result)) {
                return result;
            }
            else {
                return defValue;
            }
        }
    }
}
