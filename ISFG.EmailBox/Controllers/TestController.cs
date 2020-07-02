using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ISFG.EmailBox.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public TestController(IAlfrescoHttpClient alfrescoHttpClient) => _alfrescoHttpClient = alfrescoHttpClient;

        #endregion

        #region Public Methods

        [HttpGet("refreshg")]
        public async Task<string> Get()
        {
            //throw new BadRequestException("code", "message"); BadRequest 404
            
            Log.Error("Test Logger");

            var profile = await _alfrescoHttpClient.Profile("-me-");

            return profile.Entry.FirstName;
        }

        #endregion
    }
}
