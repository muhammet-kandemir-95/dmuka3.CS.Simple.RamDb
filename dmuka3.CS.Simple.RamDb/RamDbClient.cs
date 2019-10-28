using dmuka3.CS.Simple.TCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace dmuka3.CS.Simple.RamDb
{
    /// <summary>
    /// Client for reading and setting datas.
    /// </summary>
    public class RamDbClient : IDisposable
    {
        #region Variables
        private object lockObj = new object();

        /// <summary>
        /// Wrong protocol exception.
        /// </summary>
        private static Exception __wrongProtocolException = new Exception("Wrong protocol!");

        /// <summary>
        /// Server's host name.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Server's port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// TCP client.
        /// </summary>
        private TcpClient _client = null;

        /// <summary>
        /// For dmuka protocol.
        /// </summary>
        private TCPClientConnection _conn = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="hostName">Server's host name.</param>
        /// <param name="port">Server's port.</param>
        public RamDbClient(string hostName, int port)
        {
            this.HostName = hostName;
            this.Port = port;

            this._client = new TcpClient();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Connect the server.
        /// </summary>
        /// <param name="userName">Server authentication user name.</param>
        /// <param name="password">Server authentication password.</param>
        /// <param name="dwKeySize">SSL key size as bit.</param>
        public void Start(string userName, string password, int dwKeySize)
        {
            if (userName.Contains('<') || userName.Contains('>') || password.Contains('<') || password.Contains('>'))
                throw new Exception("UserName and Password can't containt '<' or '>'!");

            this._client.Connect(this.HostName, this.Port);
            this._conn = new TCPClientConnection(this._client);

            // SERVER : HI
            var serverHi = Encoding.UTF8.GetString(
                                this._conn.Receive()
                                );

            if (serverHi == RamDbMessages.SERVER_HI)
            {
                this._conn.StartDMUKA3RSA(dwKeySize);

                // CLIENT : HI <user_name> <password>
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        $"{RamDbMessages.CLIENT_HI} <{userName}> <{password}>"
                        ));

                var serverResAuth = Encoding.UTF8.GetString(
                                        this._conn.Receive()
                                        );

                if (serverResAuth == RamDbMessages.SERVER_NOT_AUTHORIZED)
                    // - IF AUTH FAIL
                    //      SERVER : NOT_AUTHORIZED
                    throw new Exception($"{RamDbMessages.SERVER_NOT_AUTHORIZED} - Not authorized!");
                else if (serverResAuth != RamDbMessages.SERVER_OK)
                    // - IF AUTH PASS
                    //      SERVER : OK
                    throw __wrongProtocolException;

            }
            else
                throw __wrongProtocolException;
        }

        #region Manage Datas
        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public string GetValue(string key)
        {
            lock (lockObj)
            {
                // - IF PROCESS TYPE IS "GET VALUE BY KEY"
                //      CLIENT : GET_VALUE <key>
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        $"{RamDbMessages.CLIENT_GET_VALUE} <{key}>"
                        ));


                var serverResFound = Encoding.UTF8.GetString(
                                        this._conn.Receive()
                                        );
                if (serverResFound == RamDbMessages.SERVER_NOT_FOUND)
                {
                    // - IF DATA NOT EXISTS
                    //      SERVER : NOT_FOUND
                    //      SERVER : END
                    var serverResEnd = Encoding.UTF8.GetString(
                                            this._conn.Receive()
                                            );

                    if (serverResEnd == RamDbMessages.SERVER_END)
                        return null;
                    else
                        throw __wrongProtocolException;
                }
                else if (serverResFound == RamDbMessages.SERVER_FOUND)
                {
                    // - IF DATA EXISTS
                    //      SERVER : FOUND
                    //      SERVER : data
                    var data = Encoding.UTF8.GetString(
                                            this._conn.Receive()
                                            );

                    //      SERVER : END
                    var serverResEnd = Encoding.UTF8.GetString(
                                            this._conn.Receive()
                                            );

                    if (serverResEnd == RamDbMessages.SERVER_END)
                        return data;
                    else
                        throw __wrongProtocolException;
                }
                else if (serverResFound.StartsWith(RamDbMessages.SERVER_ERROR))
                    throw new Exception(serverResFound);
                else
                    throw __wrongProtocolException;
            }
        }

        #region Get Value - Data Types
        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public bool? GetValueAsBool(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public byte? GetValueAsByte(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToByte(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public sbyte? GetValueAsSByte(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToSByte(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public short? GetValueAsInt16(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToInt16(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public ushort? GetValueAsUInt16(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToUInt16(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public int? GetValueAsInt32(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public uint? GetValueAsUInt32(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToUInt32(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public long? GetValueAsInt64(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToInt64(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public ulong? GetValueAsUInt64(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToUInt64(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public float? GetValueAsSingle(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToSingle(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public double? GetValueAsDouble(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public decimal? GetValueAsDecimal(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.ToDecimal(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public DateTime? GetValueAsDateTime(string key)
        {
            var v = this.GetValueAsInt64(key);
            if (v == null)
                return null;

            return new DateTime(v.Value);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public TimeSpan? GetValueAsTimeSpan(string key)
        {
            var v = this.GetValueAsInt64(key);
            if (v == null)
                return null;

            return new TimeSpan(v.Value);
        }

        /// <summary>
        /// Get a value from server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public byte[] GetValueAsByteArray(string key)
        {
            var v = this.GetValue(key);
            if (v == null)
                return null;

            return Convert.FromBase64String(v);
        }
        #endregion

        /// <summary>
        /// Delete a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns></returns>
        public void DeleteValue(string key)
        {
            lock (lockObj)
            {
                // - IF PROCESS TYPE IS "DELETE VALUE BY KEY"
                //      CLIENT : DELETE_VALUE <key>
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        $"{RamDbMessages.CLIENT_DELETE_VALUE} <{key}>"
                        ));


                //      SERVER : END
                var serverResEnd = Encoding.UTF8.GetString(
                                        this._conn.Receive()
                                        );

                if (serverResEnd == RamDbMessages.SERVER_END)
                    return;
                else if (serverResEnd.StartsWith(RamDbMessages.SERVER_ERROR))
                    throw new Exception(serverResEnd);
                else
                    throw __wrongProtocolException;
            }
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValue(string key, string value, TimeSpan time, out bool added)
        {
            lock (lockObj)
            {
                // - IF PROCESS TYPE IS "SET VALUE BY KEY"
                //      CLIENT : SET_VALUE <key>
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        $"{RamDbMessages.CLIENT_SET_VALUE} <{key}> <{time.Ticks.ToString(CultureInfo.InvariantCulture)}>"
                        ));

                //      CLIENT : data
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        value
                        ));


                //      SERVER : ADDED / UPDATED
                var serverResAddedUpdated = Encoding.UTF8.GetString(
                                            this._conn.Receive()
                                            );
                added = serverResAddedUpdated == RamDbMessages.SERVER_ADDED;
                if (added || serverResAddedUpdated == RamDbMessages.SERVER_UPDATED)
                {
                    //      SERVER : END
                    var serverResEnd = Encoding.UTF8.GetString(
                                            this._conn.Receive()
                                            );

                    if (serverResEnd != RamDbMessages.SERVER_END)
                        throw __wrongProtocolException;
                }
                else if (serverResAddedUpdated.StartsWith(RamDbMessages.SERVER_ERROR))
                    throw new Exception(serverResAddedUpdated);
                else
                    throw __wrongProtocolException;
            }
        }

        #region Set Value - Data Types
        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsBool(string key, bool value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsByte(string key, byte value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsSByte(string key, sbyte value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsInt16(string key, short value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsUInt16(string key, ushort value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsInt32(string key, int value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsUInt32(string key, uint value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsInt64(string key, long value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsUInt64(string key, ulong value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsSingle(string key, float value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsDouble(string key, double value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsDecimal(string key, decimal value, TimeSpan time, out bool added)
        {
            this.SetValue(key, value.ToString(CultureInfo.InvariantCulture), time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsDateTime(string key, DateTime value, TimeSpan time, out bool added)
        {
            this.SetValueAsInt64(key, value.Ticks, time, out added);
        }

        /// <summary>
        /// Set a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">New value.</param>
        /// <param name="time">Expire time.</param>
        /// <param name="added">If data will be added, it will be true. If data already exists, it will be false.</param>
        /// <returns></returns>
        public void SetValueAsTimeSpan(string key, TimeSpan value, TimeSpan time, out bool added)
        {
            this.SetValueAsInt64(key, value.Ticks, time, out added);
        }
        #endregion

        /// <summary>
        /// Increment a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Added value.</param>
        /// <returns></returns>
        public decimal IncrementValue(string key, decimal value)
        {
            lock (lockObj)
            {
                // - IF PROCESS TYPE IS "INCREMENT VALUE BY KEY"
                //      CLIENT : INCREMENT_VALUE <key>
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        $"{RamDbMessages.CLIENT_INCREMENT_VALUE} <{key}>"
                        ));

                //      CLIENT : data
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        value.ToString(CultureInfo.InvariantCulture)
                        ));


                //      SERVER : data
                var serverRes = Encoding.UTF8.GetString(
                                        this._conn.Receive()
                                        );

                decimal result;
                try
                {
                    result = Convert.ToDecimal(serverRes, CultureInfo.InvariantCulture);
                }
                catch
                {

                    if (serverRes.StartsWith(RamDbMessages.SERVER_ERROR))
                        throw new Exception(serverRes);
                    else
                        throw __wrongProtocolException;
                }

                //      SERVER : END
                var serverResEnd = Encoding.UTF8.GetString(
                                        this._conn.Receive()
                                        );

                if (serverResEnd != RamDbMessages.SERVER_END)
                    throw __wrongProtocolException;

                return result;
            }
        }

        /// <summary>
        /// Decrement a value on server by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Subtracted value.</param>
        /// <returns></returns>
        public decimal DecrementValue(string key, decimal value)
        {
            lock (lockObj)
            {
                // - IF PROCESS TYPE IS "DECREMENT VALUE BY KEY"
                //      CLIENT : DECREMENT_VALUE <key>
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        $"{RamDbMessages.CLIENT_DECREMENT_VALUE} <{key}>"
                        ));

                //      CLIENT : data
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        value.ToString(CultureInfo.InvariantCulture)
                        ));


                //      SERVER : data
                var serverRes = Encoding.UTF8.GetString(
                                        this._conn.Receive()
                                        );

                decimal result;
                try
                {
                    result = Convert.ToDecimal(serverRes, CultureInfo.InvariantCulture);
                }
                catch
                {

                    if (serverRes.StartsWith(RamDbMessages.SERVER_ERROR))
                        throw new Exception(serverRes);
                    else
                        throw __wrongProtocolException;
                }

                //      SERVER : END
                var serverResEnd = Encoding.UTF8.GetString(
                                        this._conn.Receive()
                                        );

                if (serverResEnd != RamDbMessages.SERVER_END)
                    throw __wrongProtocolException;

                return result;
            }
        }

        /// <summary>
        /// Lock a process to run on a single thread.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="time">Process time.</param>
        public void Lock(string key, TimeSpan time)
        {
            bool added;
            while (true)
            {
                this.SetValue("LOCK__" + key, "L", time, out added);
                if (added)
                    break;
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Unlock a process to run on a single thread.
        /// </summary>
        /// <param name="key">Lock key.</param>
        public void UnLock(string key)
        {
            this.DeleteValue("LOCK__" + key);
        }
        #endregion

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            try
            {
                this._conn.Send(
                    Encoding.UTF8.GetBytes(
                        $"{RamDbMessages.CLIENT_CLOSE}"
                        ));
            }
            catch
            { }
            this._conn.Dispose();
        }
        #endregion
    }
}
