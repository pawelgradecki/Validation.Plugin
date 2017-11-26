using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Odx.Xrm.Core.DataAccess;

namespace Odx.Xrm.Core
{
    public abstract class BasePlugin : IPlugin
    {
        private string unsecureConfig;
        private string secureConfig;
        private HashSet<string> availableMessages;
        private bool disableRegisterCheck;

        private enum PipelineStage
        {
            PreValidation = 10,
            PreOperation = 20,
            PostOperation = 40
        }

        protected string UnsecureConfig
        {
            get
            {
                return this.unsecureConfig;
            }
        }

        protected string SecureConfig
        {
            get
            {
                return this.secureConfig;
            }
        }

        /// <summary>
        /// Register all messages that this plugin is registered on using fluent RegisterMessage method Example:
        /// RegisterMessage<CreateRequest>().RegisterMessage<UpdateRequest>()
        /// </summary>
        protected abstract void RegisterAvailableMessages();

        private BasePlugin RegisterMessage<TMessage>(PipelineStage stage)
            where TMessage : OrganizationRequest, new()
        {
            var temp = new TMessage();
            return this.RegisterMessage(stage, temp.RequestName);
        }

        public void DisableRegisterCheck()
        {
            this.disableRegisterCheck = true;
        }

        private BasePlugin RegisterMessage(PipelineStage stage, string messageName)
        {
            this.availableMessages.Add(stage + messageName);
            return this;
        }

        public BasePlugin RegisterMessagePost(string messageName)
        {
            return this.RegisterMessage(PipelineStage.PostOperation, messageName);
        }

        public BasePlugin RegisterMessagePost<TMessage>()
            where TMessage : OrganizationRequest, new()
        {
            return this.RegisterMessage<TMessage>(PipelineStage.PostOperation);
        }

        public BasePlugin RegisterMessagePre(string messageName)
        {
            return this.RegisterMessage(PipelineStage.PreOperation, messageName);
        }

        public BasePlugin RegisterMessagePre<TMessage>()
            where TMessage : OrganizationRequest, new()
        {
            return this.RegisterMessage<TMessage>(PipelineStage.PreOperation);
        }

        public BasePlugin RegisterMessagePreValidation<TMessage>()
            where TMessage : OrganizationRequest, new()
        {
            return this.RegisterMessage<TMessage>(PipelineStage.PreValidation);
        }

        public BasePlugin RegisterMessagePreValidation(string messageName)
        {
            return this.RegisterMessage(PipelineStage.PreValidation, messageName);
        }


        public BasePlugin(string unsecureConfig, string secureConfig)
        {
            this.availableMessages = new HashSet<string>();
            this.unsecureConfig = unsecureConfig;
            this.secureConfig = secureConfig;
        }

        public virtual void Execute(IServiceProvider serviceProvider)
        {
            this.RegisterAvailableMessages();
            var context = this.GetPluginExecutionContext(serviceProvider);
            if (!this.disableRegisterCheck)
            {
                if (!availableMessages.Contains((PipelineStage)context.Stage + context.MessageName))
                {
                    throw new InvalidPluginExecutionException($"Plugin registered on bad message. Contact your system administrator");
                }
            }
        }

        protected IPluginExecutionContext GetPluginExecutionContext(IServiceProvider serviceProvider)
        {
            return (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        }
    }

    public abstract class BasePlugin<T> : BasePlugin, IPlugin
        where T : HandlerBase, IHandler, new()
    {

        public BasePlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig) { }

        public override void Execute(IServiceProvider serviceProvider)
        {
            base.Execute(serviceProvider);

            var localContext = this.GetLocalPluginContext(serviceProvider);
            var repositoryFactory = this.GetRepositoryFactory(serviceProvider);
            var tracingService = this.GetTracingService(serviceProvider);

            var handler = new T();
            handler.InitializeTracing(tracingService);
            handler.InitializeConfiguration(this.UnsecureConfig, this.SecureConfig);

            try
            {
                handler.Execute(localContext, repositoryFactory);
            }
            catch (Exception ex)
            {
                handler.Trace(ex);
                handler.Trace($"Context: {localContext.Context.InputParameters.ToJSON()}");
                throw;
            }
        }

        private IRepositoryFactory GetRepositoryFactory(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
            return new RepositoryFactory(factory);
        }

        private ILocalPluginExecutionContext GetLocalPluginContext(IServiceProvider serviceProvider)
        {
            var context = this.GetPluginExecutionContext(serviceProvider);
            return new LocalPluginExecutionContext(context);
        }

        private ITracingService GetTracingService(IServiceProvider serviceProvider)
        {
            return (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        }
    }
}