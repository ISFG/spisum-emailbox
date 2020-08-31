using ISFG.Common.Attributes;
using ISFG.EmailBox.Interfaces;

namespace ISFG.EmailBox.Models.Configuration
{
    [Settings("SpisUmEmailConfiguration")]
    public class EmailServerConfiguration : IEmailServerConfiguration
    {
        #region Implementation of IEmailServerConfiguration

        public AutomaticResponse AutomaticResponse { get; set; }

        #endregion
    }
    public class AutomaticResponse
    {
        #region Properties

        public bool SendResponse { get; set; }
        public BodyTextFile BodyTextFile { get; set; }
        public string OrganizationName { get; set; }
        public string EmailSubject { get; set; }

        #endregion
    }
    public class BodyTextFile
    {
        #region Properties

        public string Folder { get; set; }
        public string FileName { get; set; }

        #endregion
    }
}
