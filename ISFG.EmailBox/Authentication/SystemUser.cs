using ISFG.Common.Interfaces;

namespace ISFG.EmailBox.Authentication
{
    public class SystemUser : IIdentityUser
    {
        #region Implementation of IIdentityUser

        public string FirstName => "System";

        public string Group => throw new System.NotImplementedException();
        public string Id => "System";

        public bool IsAdmin => throw new System.NotImplementedException();
        public string Job { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string OrganizationAddress { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string OrganizationId { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string OrganizationName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string OrganizationUnit { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string OrganizationUserId { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string RequestGroup => throw new System.NotImplementedException();
        public string Token { get; set; }

        public string LastName => throw new System.NotImplementedException();

        #endregion
    }
}
