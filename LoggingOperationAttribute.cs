namespace smartLogger.Attributes
{
    public enum LoggingOperationConfig
    {
        Disable,
        TimeInterval,
        CountInterval,
        MaxCount,
        ReturnChangedOnly,
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed public class LoggingOperationAttribute : Attribute
    {
        readonly LoggingOperationConfig _configType;
        readonly int _configValue;

        public LoggingOperationAttribute(LoggingOperationConfig configType, int configValue = 0)
        {
            _configType = configType;
            _configValue = configValue;
        }

        public LoggingOperationConfig ConfigType { get { return _configType; } }
        public int ConfigValue { get { return _configValue; } }
    }
}
