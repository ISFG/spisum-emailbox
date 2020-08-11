using ISFG.Alfresco.Api.Interfaces;
using ISFG.EmailBox.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISFG.EmailBox.Interfaces
{
    public interface IEmailAlfrescoConfiguration : IAlfrescoConfiguration
    {
        public EmailSystemUser SystemUser { get; set; }
    }
}
