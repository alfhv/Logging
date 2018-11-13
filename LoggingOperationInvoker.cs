namespace smartLogger.Logging
{
    /// <summary>
    /// We have one instance of this class for each method of intercepted service.
    /// </summary>
    public class LoggingOperationInvoker : IOperationInvoker
    {
        private static readonly ILog Log = LogManager.GetLogger("OperationInvoker");

        private readonly IOperationInvoker _baseInvoker;
        private readonly MethodInfo _operation;
        LoggingOperationAttribute _attr;
        bool _FailedStatus; // catch exceptions in any possible method and mark as failed to disable future calls to dont crash application

        public LoggingOperationInvoker(IOperationInvoker baseInvoker, MethodInfo operation)
        {
            _baseInvoker = baseInvoker;
            _operation = operation;

            try
            {
                _attr = (LoggingOperationAttribute)_operation.GetCustomAttributes(typeof(LoggingOperationAttribute), false).FirstOrDefault();
            }
            catch(Exception e)
            {
                _FailedStatus = true;
            }
        }

        public bool IsSynchronous
        {
            get
            {
                return true;
            }
        }

        public object[] AllocateInputs()
        {
            return _baseInvoker.AllocateInputs();
        }

        /// <summary>
        /// Take control of method execution itself, usefull to globally try-catch exceptions
        /// </summary>
        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            var doLog = DoLog(); // get the configured or failed log action to do

            if (doLog == 2)
            {
                BeforeCall(inputs);
            }

            // if doLog == 0 then all logging will be ignored but the method call should be executed anyway.
            var result = _baseInvoker.Invoke(instance, inputs, out outputs);

            if (doLog == 1)
            {
                // this case is only used with ReturnChangedOnly, 
                // we can't log BeforeCall because we need to know the result first, so we only log AfterCall
                // but just if result is different than previous one.
                doLog = DoLog(result);
            }

            if (doLog == 2)
            {
                AfterCall(result);
            }

            return result;
        }

        /// <summary>
        /// Only used to check to ensure we need to log AfterCall
        /// If 
        /// </summary>
        /// <param name="result"></param>
        /// <returns>0: no log | 2: log After</returns>
        private int DoLog(object result)
        {
            if (_resultStamp != ObjectToString(result))
            {
                _resultStamp = ObjectToString(result);
                return 2;
            }

            return 0;
        }

        public object BeforeCall(object[] inputs)
        {
            try
            {
                var strInput = GetInputStr(inputs);
                Log.Info($"({GetCurrentUserName()}) Calling {_operation.Name}({strInput})");
            }
            catch(Exception e)
            {
                _FailedStatus = true;
            }

            return null;
        }

        public void AfterCall(object returnValue)
        {
            try
            {
                var returnValueToString = ObjectToString(returnValue);

                Log.InfoFormat("({2}) Operation {0} return: {1}", _operation.Name, returnValueToString, GetCurrentUserName());
            }
            catch(Exception e)
            {
                _FailedStatus = true;
            }
        }

        private object GetInputStr(object[] inputs)
        {
            var result = inputs.Select(i => ObjectToString(i));

            return string.Join(", ", result);
        }

        private static string ObjectToString(object elem)
        {
            if (elem == null)
            {
                return "(null)";
            }

            if (elem.GetType().IsPrimitive || elem.GetType() == typeof(String) || elem.GetType() == typeof(Decimal)) // TODO: improve logging of type class
            {
                return elem.ToString();
            }

            if (elem is IList)
            {
                return $"{((IList)elem).Count} items";
            }

            if (elem is Enum)
            {
                return elem.ToString();
            }

            if (elem is DateTime)
            {
                return ((DateTime)elem).ToShortDateString();
            }

            return elem.GetType().ToString();
        }

        public static string GetCurrentUserName()
        {
            return BartService.GetCurrentUserName();
        }

        private DateTime _timeStamp;
        private int _countStamp;
        private string _resultStamp;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>0: no log | 1: log only AfterCall | 2: log BeforeCall and AfterCall</returns>
        private int DoLog()
        {
            if (_FailedStatus)
            {
                // if exception found in any method, then permanent disable logs for this instance
                return 0;
            }

            if (_attr == null)
                return 2;

            if (_attr.ConfigType == LoggingOperationConfig.Disable)
                return 0;

            if (_attr.ConfigType == LoggingOperationConfig.ReturnChangedOnly)
            {
                return 1;
            }

            if (_attr.ConfigType == LoggingOperationConfig.TimeInterval)
            {
                if (_timeStamp == null || _timeStamp == DateTime.MinValue)
                {
                    // first call
                    _timeStamp = DateTime.Now;
                    return 2;
                }
                else
                {
                    if ((DateTime.Now - _timeStamp).Seconds > _attr.ConfigValue)
                    {
                        _timeStamp = DateTime.Now;
                        return 2;
                    }
                }
            }

            if (_attr.ConfigType == LoggingOperationConfig.MaxCount)
            {
                if (_countStamp++ < _attr.ConfigValue)
                    return 2;
            }
            
            return 0;
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            throw new NotImplementedException();
        }
    }
}
