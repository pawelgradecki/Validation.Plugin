using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Odx.Xrm.Core;

namespace Validation.Plugin.CoreLib.Model
{
    public class CrmEntity : Entity
    {
        private Dictionary<string, string> logicalNamesDictionary;

        public CrmEntity()
        {
            this.LogicalName = this.GetType().GetCustomAttribute<EntityLogicalNameAttribute>().LogicalName;
            this.logicalNamesDictionary = this.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetCustomAttribute<AttributeLogicalNameAttribute>()?.LogicalName);
        }

        protected string GetAttributeName(string propertyName)
        {
            return this.logicalNamesDictionary[propertyName] ?? propertyName.ToLowerInvariant();
        }

        protected T GetAttribute<T>([CallerMemberName]string propertyName = null)
        {
            return this.GetAttributeValue<T>(GetAttributeName(propertyName));
        }

        protected void SetAttribute(object value, [CallerMemberName]string propertyName = null)
        {
            this.SetAttributeValue(GetAttributeName(propertyName), value);
        }
    }
}
