using System.Collections.Generic;
using MimeKit;

namespace ISFG.EmailBox.Models
{
    public class EmailHistory
    {
        #region Fields

        public List<MimeMessage> Messages = new List<MimeMessage>();

        #endregion

        #region Properties

        public int Id { get; set; }

        #endregion
    }
}
