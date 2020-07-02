using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.EmailBox.Interfaces;
using ISFG.EmailBox.Models;
using ISFG.EmailBox.Services;
using ISFG.Emails.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.EmailBox.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        #region Fields

        private readonly EmailService _emailProvider;

        #endregion

        #region Constructors

        public EmailController(IAlfrescoHttpClient alfrescoHttpClient, IEmailServerConfiguration emailServerConfiguration) => 
            _emailProvider = new EmailService(alfrescoHttpClient, emailServerConfiguration);

        #endregion

        #region Public Methods

        [HttpGet("accounts")]
        public async Task<List<EmailAccount>> Accounts() => 
            await _emailProvider.GetConfigurationEmails();

        [HttpPost("refresh")]
        public async Task<int> Refresh() => 
            await _emailProvider.Refresh();

        [HttpPost("send")]             
        public async Task<SendResponse> Send(string to, string from, string subject, string htmlbody, List<IFormFile> attachments) => 
            await _emailProvider.SendEmail(to, from, subject, htmlbody, attachments);

        [HttpGet("status")]
        public async Task<EmailStatusResult> Status(int id) => 
            await _emailProvider.Status(id);

        #endregion

        #region Nested Types, Enums, Delegates

        public class BodyTest
        {
            #region Properties

            public string To { get; set; }
            public string From { get; set; }
            public string Subject { get; set; }
            public string HtmlBody { get; set; }
            public List<IFormFile> Attachments { get; set; }

            #endregion
        }

        #endregion
    }
}
