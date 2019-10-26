using System;
using System.Collections.Generic;
using System.Text;

namespace dmuka3.CS.Simple.RamDb
{
    /// <summary>
    /// Protocol messages.
    /// </summary>
    internal static class RamDbMessages
    {
        #region Variables
        internal const string SERVER_HI = "HI";
        internal const string SERVER_NOT_AUTHORIZED = "NOT_AUTHORIZED";
        internal const string SERVER_OK = "OK";
        internal const string SERVER_NOT_FOUND = "NOT_FOUND";
        internal const string SERVER_FOUND = "FOUND";
        internal const string SERVER_ADDED = "ADDED";
        internal const string SERVER_UPDATED = "UPDATED";
        internal const string SERVER_END = "END";
        internal const string SERVER_ERROR = "ERROR";

        internal const string CLIENT_HI = "HI";
        internal const string CLIENT_GET_VALUE = "GET_VALUE";
        internal const string CLIENT_SET_VALUE = "SET_VALUE";
        internal const string CLIENT_DELETE_VALUE = "DELETE_VALUE";
        internal const string CLIENT_INCREMENT_VALUE = "INCREMENT_VALUE";
        internal const string CLIENT_DECREMENT_VALUE = "DECREMENT_VALUE";
        internal const string CLIENT_CLOSE = "CLOSE";
        #endregion
    }
}
