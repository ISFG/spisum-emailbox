using System;

namespace ISFG.EmailBox.Extensions
{
    public static class DateTimeExtension
    {
        #region Static Methods

        public static long ToUnixTimeStamp(this DateTime dateTime) => 
            new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

        #endregion
    }
}
