using System;

namespace ISFG.EmailBox.Extensions
{
    public static class LongExtension
    {
        #region Static Methods

        public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp)
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp);
            return dateTimeOffset.ToUniversalTime().UtcDateTime;
        }

        #endregion
    }
}
