using System;
using System.IO;
using System.Net;
using System.Text;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.AuthApi;
using ISFG.Common.Interfaces;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using RestSharp;

namespace ISFG.EmailBox.Authentication
{
    public class SystemAuthentication : IAuthenticationHandler
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;
        private readonly IHttpUserContextService _userContextService;

        #endregion

        #region Constructors

        public SystemAuthentication(IAlfrescoConfiguration alfrescoConfiguration,
            IHttpUserContextService userContextService)
        {
            _alfrescoConfiguration = alfrescoConfiguration;
            _userContextService = userContextService;
        }

        #endregion

        #region Implementation of IAuthenticationHandler

        public async void AuthenticateRequest(IRestRequest request)
        {
            if (!string.IsNullOrEmpty(_userContextService.Current.Token))
                request.AddHeader(HeaderNames.Authorization, _userContextService.Current.Token);
        }

        public bool HandleNotAuthenticated()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(new Uri(_alfrescoConfiguration.Url),
                "alfresco/api/-default-/public/authentication/versions/1/tickets"));
            webRequest.Method = "POST";
            webRequest.Timeout = 15000;
            webRequest.ContentType = "application/json";

            try
            {
                CreateRequestBody(webRequest);

                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                if (response?.StatusCode != HttpStatusCode.Created)
                    return false;

                var responseModel = CreateResponse(response);

                if (responseModel?.Entry?.Id == null)
                    return false;

                _userContextService.Current.Token = $"Basic {responseModel.Entry.Id.ToAlfrescoAuthentication()}";
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void CreateRequestBody(HttpWebRequest webRequest)
        {
            var sendData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new TicketBody
            {
                UserId = _alfrescoConfiguration.SystemUser.Username,
                Password = _alfrescoConfiguration.SystemUser.Password
            }));

            webRequest.ContentLength = sendData.Length;

            Stream newStream = webRequest.GetRequestStream();
            newStream.Write(sendData, 0, sendData.Length);
            newStream.Close();
        }

        private TicketEntry CreateResponse(HttpWebResponse response)
        {
            using Stream responseData = response.GetResponseStream();
            using StreamReader streamReader = new StreamReader(responseData);

            string contributorsAsJson = streamReader.ReadToEnd();

            return JsonConvert.DeserializeObject<TicketEntry>(contributorsAsJson,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
        }

        #endregion
    }
}
