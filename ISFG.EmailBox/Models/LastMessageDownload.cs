using System.Collections.Generic;

namespace ISFG.EmailBox.Models
{
    public class LastMessageDownload
    {
        #region Properties

        public long DownloadTimestamp { get; set; }
        public long Timestamp { get; set; }
        public List<string> MessageIds { get; set; } = new List<string>();

        #endregion
    }
}
