using ISFG.EmailBox.Models.Configuration;

namespace ISFG.EmailBox.Interfaces
{
    public interface IEmailServerConfiguration
    {
        #region Properties

        public AutomaticResponse AutomaticResponse { get; set; }

        #endregion
    }    
}
