namespace ISFG.EmailBox.Models
{
    public class EmailAccount
    {
        #region Constructors

        public EmailAccount(string username, string name)
        {
            Username = username;
            Name = name;
        }

        #endregion

        #region Properties

        public string Username { get; }
        public string Name { get; }

        #endregion
    }
}
