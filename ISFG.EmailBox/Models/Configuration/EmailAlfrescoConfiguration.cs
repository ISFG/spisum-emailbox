using ISFG.Alfresco.Api.Configurations;
using ISFG.Common.Attributes;
using ISFG.EmailBox.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISFG.EmailBox.Models.Configuration
{
    [Settings("Alfresco")]
    public class EmailAlfrescoConfiguration : IEmailAlfrescoConfiguration
    {
        public ConfigurationFiles ConfigurationFiles { get; set; }

        public string Groups { get; set; }

        public string Roles { get; set; }

        public string ShreddingPlan { get; set; }

        public string SiteRM { get; set; }

        public string Sites { get; set; }

        public uint? TokenExpire { get; set; }

        public string Url { get; set; }

        public string Users { get; set; }

        public EmailSystemUser SystemUser { get; set; }
    }
}
