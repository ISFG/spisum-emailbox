using System.Threading.Tasks;

namespace ISFG.EmailBox.Interfaces
{
    public interface IEmailService
    {
        #region Public Methods

        Task StartAutomaticDownload();

        #endregion
    }
}
