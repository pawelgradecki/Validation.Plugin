using System;
using System.Linq;
using System.Collections;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Odx.Xrm.Core;
using Odx.Xrm.Core.DataAccess;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;

namespace Validation.Plugin.CoreLib.BusinessLogic.Handlers.Accounts
{
    public class ValidationHandler : HandlerBase, IHandler
    {
        public void Execute(ILocalPluginExecutionContext localContext, IRepositoryFactory repoFactory)
        {
            if (this.CheckIfUserIsAdmin(localContext, repoFactory))
            {
                return;
            }

            var entity = GetEntityFromContext(localContext);

            var metadataRequest = new RetrieveEntityRequest()
            {
                LogicalName = entity.LogicalName,
                EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Attributes
            };

            var allRequredAttributes = GetAllRequiredAttributes(repoFactory, metadataRequest);

            foreach (var attribute in allRequredAttributes)
            {
                var errorMessage = $"Attribute {attribute.LogicalName} is required. Please specify value for this attribute before saving record.";
                if (!entity.Contains(attribute.LogicalName))
                {
                    throw new InvalidPluginExecutionException(errorMessage);
                }
                else
                {
                    var value = entity[attribute.LogicalName];
                    if (value == null)
                    {
                        throw new InvalidPluginExecutionException(errorMessage);
                    }
                    else if (value is string && string.IsNullOrEmpty(value as string))
                    {
                        throw new InvalidPluginExecutionException(errorMessage);
                    }
                }
            }
        }

        private IEnumerable<AttributeMetadata> GetAllRequiredAttributes(IRepositoryFactory repoFactory, RetrieveEntityRequest metadataRequest)
        {
            var generalRepository = repoFactory.Get<IBaseRepository>();
            var metadata = generalRepository.Execute<RetrieveEntityRequest, RetrieveEntityResponse>(metadataRequest);
            var attributeMetadataCollection = metadata.EntityMetadata.Attributes;
            var allRequredAttributes = attributeMetadataCollection.Where(am => am.RequiredLevel.Value == AttributeRequiredLevel.ApplicationRequired);
            return allRequredAttributes;
        }

        private Entity GetEntityFromContext(ILocalPluginExecutionContext localContext)
        {
            var target = localContext.TargetEntity;
            var entity = new Entity(target.LogicalName);
            if (localContext.HasPreImage)
            {
                var preImage = localContext.PreImage;
                foreach (var preImageAttribute in preImage.Attributes)
                {
                    entity[preImageAttribute.Key] = target.Contains(preImageAttribute.Key) ? target[preImageAttribute.Key] : preImageAttribute;
                }
            }

            foreach (var targetAttribute in target.Attributes)
            {
                entity[targetAttribute.Key] = targetAttribute.Value;
            }

            return entity;
        }

        private bool CheckIfUserIsAdmin(ILocalPluginExecutionContext localContext, IRepositoryFactory repoFactory)
        {
            var generalRepository = repoFactory.Get<IBaseRepository>();

            return generalRepository.IsAdmin(localContext.Context.InitiatingUserId);
        }
    }
}
