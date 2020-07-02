using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Utils;
using ISFG.EmailBox.Extensions;
using ISFG.EmailBox.Interfaces;
using ISFG.EmailBox.Models;
using ISFG.Emails;
using ISFG.Emails.Interface;
using ISFG.Emails.Models;
using ISFG.Exceptions.Exceptions;
using MailKit.Net.Pop3;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using Serilog;
using Parameter = RestSharp.Parameter;

namespace ISFG.EmailBox.Services
{
    public class EmailService : IEmailService
    {
        #region Fields

        private static List<EmailConfiguration> _configuration = new List<EmailConfiguration>();
        private static readonly List<EmailHistory> History = new List<EmailHistory>();
        private static readonly string TimeStampProperty = "ssl:timestampText";
        private readonly IAlfrescoHttpClient _alfrescoHttp;
        private readonly IEmailServerConfiguration _emailServerConfiguration;
        private List<Task<List<MimeMessage>>> _downloadingTask = new List<Task<List<MimeMessage>>>();

        #endregion

        #region Constructors

        public EmailService(IAlfrescoHttpClient alfrescoHttp, IEmailServerConfiguration emailServerConfiguration)
        {
            _alfrescoHttp = alfrescoHttp;
            _emailServerConfiguration = emailServerConfiguration;
        }

        #endregion

        #region Properties

        private int AutomaticDownloadDelay { get; } = 30; // In minutes
        private static int CurrentDownloadId { get; set; }
        private static int CurrentDownloadedMessagesCount { get; set; }
        private static bool IsRunning { get; set; }
        private static long DownloadTimestamp { get; set; }
        public static bool CancelationToken { get; set; } = false;
        private static Dictionary<string, LastMessageDownload> NewTimestampText { get; set; }
        public static NodeEntry EmailFolder { get; set; }

        #endregion

        #region Implementation of IEmailService

        public async Task StartAutomaticDownload()
        {
            while (!CancelationToken)
            {
                if (!IsRunning)
                // Don't want to await
                    CreateDownloadTasks();

                await Task.Delay(AutomaticDownloadDelay * 1000 * 60);
            }
        }

        #endregion

        #region Public Methods

        public async Task<List<EmailAccount>> GetConfigurationEmails()
        {
            List<EmailAccount> emailAccounts = new List<EmailAccount>();

            await Task.Run(() =>
            {
                emailAccounts.AddRange(_configuration.Select(c => new EmailAccount(c.Username, c.DisplayName)));
            });

            return emailAccounts;
        }

        public async Task<int> Refresh()
        {
            await Task.Run(async () =>
            {
                if (!IsRunning)
                    CreateDownloadTasks();
            });

            return CurrentDownloadId;
        }

        public async Task<SendResponse> SendEmail(string to, string from, string subject, string htmlBody, List<IFormFile> attachments = null)
        {    
            try
            {
                if (string.IsNullOrWhiteSpace(to)) throw new BadRequestException("", "Email of addressee is mandatory.");
                if (string.IsNullOrWhiteSpace(subject)) throw new BadRequestException("", "Subject is mandatory.");

                var emailConfiguration = _configuration.FirstOrDefault(x => x?.Username?.ToLower() == from?.ToLower())
                    ?? throw new BadRequestException("", "Provided email is not in configuration.");

                var provider = new EmailProvider(emailConfiguration);
                var result = await provider.Send(to, subject, htmlBody, attachments);

                if (result.Exception != null) Log.Error(result.Exception, "Email automatic response could not be sended.");

                return result;
            }
            catch (Exception e)
            {
                return new SendResponse(false, e);
            }            
        }

        public async Task<EmailStatusResult> Status(int id)
        {
            EmailStatusResult status = new EmailStatusResult();

            await Task.Run(() =>
            {
                if (id == CurrentDownloadId)
                {
                    status = new EmailStatusResult { Running = IsRunning, NewMessageCount = CurrentDownloadedMessagesCount };
                }
                else
                {

                    var result = History.Exists(x => x.Id == id);

                    if (!result)
                        throw new BadRequestException("", "ID doesn't exists");
                    status = new EmailStatusResult { Running = false, NewMessageCount = History.Where(x => x.Id == id).First().Messages.Count() };
                }
            });

            return status;
        }

        #endregion

        #region Private Methods

        private async Task CreateAllSecondaryChildren(NodeEntry emlFile, List<NodeEntry> attachments)
        {
            foreach (var attachment in attachments) await CreateSecondaryChildren(emlFile, attachment);
        }

        private async Task CreateDownloadTasks()
        {
            ReadConfiguration();

            if (_configuration == null || _configuration.Count == 0)
                return;

            IsRunning = true;
            _downloadingTask = new List<Task<List<MimeMessage>>>();
            CurrentDownloadId++;
            CurrentDownloadedMessagesCount = 0;

            DownloadTimestamp = DateTime.Now.ToUnixTimeStamp();

            foreach (var email in _configuration) _downloadingTask.Add(Task.Factory.StartNew(() => StartDownloadProcess(email)));

            // Wait until all download is compleated
            await Task.WhenAll(_downloadingTask);

            EmailHistory lastHistory = new EmailHistory { Id = CurrentDownloadId };
            foreach (var task in _downloadingTask)
            {
                if (task.Result == null)
                    continue;

                lastHistory.Messages.AddRange(task.Result);
            }
            History.Add(lastHistory);

            IsRunning = false;
        }

        private async Task CreateSecondaryChildren(NodeEntry message, NodeEntry attachment)
        {
            await _alfrescoHttp.CreateNodeSecondaryChildren(message.Entry.Id, new ChildAssociationBody
            {
                AssocType = "ssl:emailAttachments",
                ChildId = attachment.Entry.Id
            });
        }

        private async Task DeleteNode(NodeEntry entry)
        {
            await DeleteNodes(new List<NodeEntry> { entry });
        }

        private async Task DeleteNodes(List<NodeEntry> entries)
        {
            foreach (var entry in entries)
                try
                {
                    await _alfrescoHttp.DeleteNode(entry.Entry.Id);
                }
                catch { }
        }
        
        private string GetAutomaticResponseBodyText(string filePath, List<ReplaceTexts> replaceTexts)
        {
            var scriptText = File.ReadAllText(filePath);

            replaceTexts.ForEach(x =>
            {
                scriptText = scriptText.Replace(x.ReplaceText, x.ReplaceWithText);
            });

            return scriptText;
        }

        private Dictionary<string, LastMessageDownload> GetTimestampProperty(NodeEntry _entry)
        {
            // Save timestamp of last download
            try
            {
                var properties = _entry.Entry.Properties.As<JObject>().ToDictionary();
                var timestampText = properties.GetNestedValueOrDefault(TimeStampProperty)?.ToString();

                try
                {
                    return timestampText != null ? JsonConvert.DeserializeObject<Dictionary<string, LastMessageDownload>>(timestampText) : new Dictionary<string, LastMessageDownload>();
                }
                catch
                {
                    return new Dictionary<string, LastMessageDownload>();
                }
            }
            catch
            {
                return new Dictionary<string, LastMessageDownload>();
            }
        }

        private bool IsMessageAlreadyDownloaded(string email, MimeMessage message)
        {
            var info = NewTimestampText.GetValueOrDefault(email);

            if (info == null || info.Timestamp.UnixTimeStampToDateTime() < message.Date)
                return false;

            if (info.Timestamp.UnixTimeStampToDateTime() == message.Date)
            {
                if (info.MessageIds.Contains(message.MessageId))
                    return true;
                return false;
            }

            return false;
        }

        private void ReadConfiguration()
        {
            try
            {
                _configuration = JsonConvert.DeserializeObject<List<EmailConfiguration>>(File.ReadAllText(
                                Path.Combine(AppContext.BaseDirectory, "ConfigurationFiles", "EmailConfiguration.json"), Encoding.Default)
                                );
            }
            catch
            {

            }
        }

        private async Task<List<MimeMessage>> RecieveEmails(IEmailConfiguration configuration)
        {
            List<MimeMessage> emails = new List<MimeMessage>();
            try
            {
                EmailFolder = await _alfrescoHttp.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.RelativePath, "Sites/Mailroom/documentLibrary/MailBox/Unprocessed", ParameterType.QueryString)));

                NewTimestampText = GetTimestampProperty(EmailFolder);

                using (var emailClient = new Pop3Client())
                {
                    emailClient.Connect(configuration.Pop3.Host, configuration.Pop3.Port, configuration.Pop3.UseSSL);
                    // Remove OAUTH2 because we dont use it right now.
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(configuration.Username, configuration.Password);

                    for (int i = 0; i < emailClient.Count; i++)
                        using (var memory = new MemoryStream())
                        {
                            var message = emailClient.GetMessage(i);

                            // Check if message was downloaded
                            if (IsMessageAlreadyDownloaded(configuration.Username, message))
                                continue;

                            message.WriteTo(memory);

                            // If third parameter is set - It will end up with badrequest
                            var file = new FormDataParam(memory.ToArray(), $"{IdGenerator.GenerateId()}.eml", null, "message/rfc822");

                            var attachments = await SaveAllAttachments(message.Attachments);

                            if (attachments == null)
                            {
                                emailClient.Disconnect(false);
                                return emails;
                            }

                            var emlFile = await SaveEMLFile(message, file, attachments);

                            if (emlFile == null)
                            {
                                // Delete all attachments if eml fails to upload
                                await DeleteNodes(attachments);
                                emailClient.Disconnect(false);
                                return emails;
                            }

                            try
                            {
                                await CreateAllSecondaryChildren(emlFile, attachments);
                            }
                            catch
                            {
                                // Delete .eml file and it's attachments if ucreation of secondary childrens fails
                                await DeleteNode(emlFile);
                                await DeleteNodes(attachments);
                                emailClient.Disconnect(false);
                                return emails;
                            }

                            EmailProvider provider = new EmailProvider(configuration);
                            
                            try
                            {
                                var body = GetAutomaticResponseBodyText(
                                    Path.Combine(_emailServerConfiguration.AutomaticResponse.BodyTextFile.Folder,
                                        _emailServerConfiguration.AutomaticResponse.BodyTextFile.FileName),
                                    new List<ReplaceTexts>
                                    {
                                        new ReplaceTexts("[predmet_doruceneho_emailu]", message.Subject),
                                        new ReplaceTexts("[nazev_organizace]", _emailServerConfiguration.AutomaticResponse.OrganizationName)
                                    });

                                await SendEmail(
                                    message?.From?.Mailboxes?.FirstOrDefault()?.Address,
                                    configuration.Username,
                                    _emailServerConfiguration?.AutomaticResponse?.EmailSubject,
                                    body);
                            }
                            catch (Exception e)
                            {
                                
                            }

                            // if succesfully is uploaded
                            emails.Add(emailClient.GetMessage(i));
                            CurrentDownloadedMessagesCount++;
                            await SaveTimeStamp(configuration.Username, message);
                        }

                    // update download time anyway
                    await SaveTimeStamp(configuration.Username);

                    // Disconect marks emails as "downloaded" and they will not appear next time                    
                    emailClient.Disconnect(true);
                    return emails;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return emails;
            }
        }

        private async Task<List<NodeEntry>> SaveAllAttachments(IEnumerable<MimeEntity> attachments)
        {
            List<NodeEntry> createdAttachments = new List<NodeEntry>();

            foreach (var attachment in attachments)
                try
                {
                    createdAttachments.Add(await SaveAttachment(attachment));
                }
                catch
                {
                    // Delete uploaded nodes if one failes to upload
                    await DeleteNodes(createdAttachments);
                    return null;
                }

            return createdAttachments;
        }

        private async Task<NodeEntry> SaveAttachment(MimeEntity attachment)
        {
            using (var memoryAttachment = new MemoryStream())
            {
                if (attachment is MimePart)
                    ((MimePart)attachment).Content.DecodeTo(memoryAttachment);
                else
                    ((MessagePart)attachment).Message.WriteTo(memoryAttachment);

                var bytes = memoryAttachment.ToArray();

                var Title = $"{IdGenerator.GenerateId()}{ Path.GetExtension(attachment.ContentDisposition?.FileName)}";

                FormDataParam fileParamsAttachment = new FormDataParam(bytes, Title);

                return await _alfrescoHttp.CreateNode(EmailFolder.Entry.Id, fileParamsAttachment, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.NodeType, "cm:content", ParameterType.GetOrPost))
                    .Add(new Parameter(AlfrescoNames.ContentModel.Title, Title, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:fileName", attachment.ContentDisposition?.FileName, ParameterType.GetOrPost))
                    .Add(new Parameter(HeaderNames.ContentType, "multipart/form-data", ParameterType.HttpHeader))
                    );
            }
        }

        private async Task<NodeEntry> SaveEMLFile(MimeMessage message, FormDataParam fileParams, List<NodeEntry> attachments)
        {
            try
            {
                return await _alfrescoHttp.CreateNode(EmailFolder.Entry.Id, fileParams, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(HeaderNames.ContentType, "multipart/form-data", ParameterType.HttpHeader))
                    .Add(new Parameter(AlfrescoNames.Headers.NodeType, "ssl:email", ParameterType.GetOrPost))
                    .Add(new Parameter(AlfrescoNames.ContentModel.Title, message.MessageId, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:fileName", $"{message.Subject}.eml", ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailAttachmentsCount", attachments?.Count ?? 0, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailDeliveryDate", message.Date.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"), ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailMessageId", message.MessageId, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailRecipient", message.To.Mailboxes.First().Address, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailRecipientName", message.To.Mailboxes.First().Name, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailSender", message.From.Mailboxes.First().Address, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailSenderName", message.From.Mailboxes.First().Name, ParameterType.GetOrPost))
                    .Add(new Parameter("ssl:emailSubject", message.Subject, ParameterType.GetOrPost)));
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveTimeStamp(string email, MimeMessage message = null)
        {
            var info = NewTimestampText.GetValueOrDefault(email);

            if (message != null && (info == null || info.Timestamp != message.Date.ToUnixTimeMilliseconds()))
            {
                NewTimestampText[email] = new LastMessageDownload
                {
                    DownloadTimestamp = DownloadTimestamp,
                    MessageIds = new List<string> { message.MessageId },
                    Timestamp = message.Date.ToUnixTimeMilliseconds()
                };
            }
            else
            {
                if (message != null)
                    info.MessageIds.Add(message.MessageId);

                NewTimestampText[email] = new LastMessageDownload
                {
                    DownloadTimestamp = DownloadTimestamp,
                    MessageIds = info.MessageIds,
                    Timestamp = info.Timestamp
                };
            }

            await _alfrescoHttp.UpdateNode(EmailFolder.Entry.Id, new NodeBodyUpdate()
                .AddProperty(TimeStampProperty, JsonConvert.SerializeObject(NewTimestampText, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                })));
        }

        private List<MimeMessage> StartDownloadProcess(IEmailConfiguration configuration) => 
            RecieveEmails(configuration).Result;

        #endregion

        #region Nested Types, Enums, Delegates

        private class ReplaceTexts
        {
            #region Constructors

            public ReplaceTexts(string replaceText, string replaceWithText)
            {
                ReplaceText = replaceText;
                ReplaceWithText = replaceWithText;
            }

            #endregion

            #region Properties

            public string ReplaceText { get; }
            public string ReplaceWithText { get; }

            #endregion
        }

        #endregion
    }
}

