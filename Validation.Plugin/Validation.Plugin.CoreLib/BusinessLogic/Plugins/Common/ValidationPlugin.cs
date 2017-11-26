using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Odx.Xrm.Core;
using Validation.Plugin.CoreLib.BusinessLogic.Handlers.Accounts;

namespace Validation.Plugin.CoreLib.Plugins.Accounts
{
    public class ValidationPlugin : BasePlugin<ValidationHandler>, IPlugin
    {
        public ValidationPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(unsecureConfiguration, secureConfiguration) { }

        protected override void RegisterAvailableMessages()
        {
            this.DisableRegisterCheck();
        }
    }
}
