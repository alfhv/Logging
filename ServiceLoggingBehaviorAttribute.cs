namespace smartLogger.Logging
{
    /// <summary>
    /// The interceptor behavior.
    /// It is attached to service by unity.wcf auto-wiring all IServiceBehavior instances found in application. 
    /// Can be used as attribute also(but wont have access to Unity container then)
    /// </summary>
    public class ServiceLoggingBehaviorAttribute : Attribute, IServiceBehavior
    {
        private static readonly ILog Log = LogManager.GetLogger("ServiceLoggingBehaviorAttribute");

        public ServiceLoggingBehaviorAttribute()
        {
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
            
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ServiceEndpoint endpoint in serviceDescription.Endpoints)
            {
                foreach (OperationDescription operation in endpoint.Contract.Operations)
                {
                    /* deactivate "webservice logger for smoke test" for now...
                    IOperationBehavior dbLogBehavior = new WebServiceCallLoggerBehavior();
                    operation.Behaviors.Add(dbLogBehavior);
                    Log.Debug($"WebServiceCallLoggerBehavior added to operation: {operation.Name}");
                    */

                    var logAttr = operation.SyncMethod.GetCustomAttributes(typeof(LoggingOperationAttribute), false).FirstOrDefault();

                    if (logAttr != null && ((logAttr as LoggingOperationAttribute).ConfigType == LoggingOperationConfig.Disable))
                        continue; // dont attach behavior if we dont want logs for this operation

                    IOperationBehavior behavior = new LoggingOperationBehavior();
                    operation.Behaviors.Add(behavior);
                    Log.Debug($"LoggingOperationBehavior added to operation: {operation.Name}");
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            
        }
    }
}
