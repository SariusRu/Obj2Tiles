using System.Reflection;
using log4net;
using log4net.Config;

namespace Obj2Tiles.Common
{
    /// <summary>
    /// The logging API.
    /// </summary>
    public static class Logging
    {
        #region Fields
        private static ILog _logger = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the active logger.
        /// </summary>
        public static ILog Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }
        #endregion

        #region Static Initializer
        /// <summary>
        /// The static initializer.
        /// </summary>
        static Logging()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        }
        #endregion

        #region Public Methods
        #region Verbose
        /// <summary>
        /// Logs a line of verbose text.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void Info(string text)
        {
            _logger.Info(text);
        }
        #endregion

        #region Warnings
        /// <summary>
        /// Logs a line of warning text.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void Warn(string text)
        { 
            _logger.Warn(text);
        }
        #endregion

        #region Errors
        /// <summary>
        /// Logs a line of error text.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void Error(string text)
        {
            _logger.Error(text);
        }
        
        public static void Error(string text, Exception ex)
        {
            _logger.Error(text, ex);
        }
        #endregion
        #endregion
    }
}