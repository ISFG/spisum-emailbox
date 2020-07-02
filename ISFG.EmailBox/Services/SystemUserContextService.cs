using ISFG.Common.Interfaces;
using ISFG.EmailBox.Authentication;

namespace ISFG.EmailBox.Services
{
    public class SystemUserContextService : IHttpUserContextService
    {
        #region Implementation of IHttpUserContextService

        public IIdentityUser Current { get; } = new SystemUser();

        #endregion
    }
}
