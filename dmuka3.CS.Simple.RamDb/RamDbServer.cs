using dmuka3.CS.Simple.RSA;
using dmuka3.CS.Simple.Semaphore;
using dmuka3.CS.Simple.TCP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace dmuka3.CS.Simple.RamDb
{
    /// <summary>
    /// Server for store datas.
    /// </summary>
    public class RamDbServer : IDisposable
    {
        #region Variables
        /// <summary>
        /// Server port.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Time out auth as second.
        /// </summary>
        public int TimeOutAuth { get; private set; }

        /// <summary>
        /// Server authentication user name.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Server authentication password.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Maximum connection count at the same time processing.
        /// </summary>
        public int CoreCount { get; private set; }

        /// <summary>
        /// SSL key size as bit.
        /// </summary>
        public int SSLDwKeySize { get; private set; }

        /// <summary>
        /// Server.
        /// </summary>
        private TcpListener _listener = null;

        /// <summary>
        /// Manage the semaphore for connections.
        /// </summary>
        private ActionQueue _actionQueueConnections = null;

        /// <summary>
        /// Cache datas.
        /// </summary>
        public ConcurrentDictionary<string, string> DbValues { get; private set; } = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Lifetimes of cache datas.
        /// </summary>
        public ConcurrentDictionary<string, DateTime> DbValuesExpire { get; private set; } = new ConcurrentDictionary<string, DateTime>();
        #endregion

        #region Constructors
        /// <summary>
        /// Instance server.
        /// </summary>
        /// <param name="userName">Server authentication user name.</param>
        /// <param name="password">Server authentication password.</param>
        /// <param name="sslDwKeySize">SSL key size as bit.</param>
        /// <param name="coreCount">Maximum connection count at the same time processing.</param>
        /// <param name="port">Server port.</param>
        /// <param name="timeOutAuth">Time out auth as second.</param>
        public RamDbServer(string userName, string password, int sslDwKeySize, int coreCount, int port, int timeOutAuth = 1)
        {
            this.UserName = userName;
            this.Password = password;
            this.TimeOutAuth = timeOutAuth;
            this.SSLDwKeySize = sslDwKeySize;
            this._listener = new TcpListener(IPAddress.Any, port);
            this._actionQueueConnections = new ActionQueue(coreCount);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Start the server as sync.
        /// </summary>
        public void Start()
        {
            this._actionQueueConnections.Start();
            this._listener.Start();
            var rsaServer = new RSAKey(this.SSLDwKeySize);

            while (true)
            {
                TcpClient client = null;

                try
                {
                    client = this._listener.AcceptTcpClient();
                }
                catch
                {
                    break;
                }

                this._actionQueueConnections.AddAction(() =>
                {
                    try
                    {
                        var conn = new TCPClientConnection(client);
                        // SERVER : HI
                        conn.Send(
                            Encoding.UTF8.GetBytes(
                                RamDbMessages.SERVER_HI
                                ));

                        // CLIENT : public_key
                        var clientPublicKey = Encoding.UTF8.GetString(
                                                conn.Receive(maxPackageSize: 10240, timeOutSecond: this.TimeOutAuth)
                                                );
                        var rsaClient = new RSAKey(clientPublicKey);

                        // SERVER : public_key
                        conn.Send(
                            rsaClient.Encrypt(
                                Encoding.UTF8.GetBytes(
                                    rsaServer.PublicKey
                                    )));

                        // CLIENT : HI <user_name> <password>
                        var clientHi = Encoding.UTF8.GetString(
                                            rsaServer.Decrypt(conn.Receive(timeOutSecond: this.TimeOutAuth))
                                            );
                        var splitClientHi = clientHi.Split('<');
                        var clientHiUserName = splitClientHi[1].Split('>')[0];
                        var clientHiPassword = splitClientHi[2].Split('>')[0];
                        if (this.UserName != clientHiUserName || this.Password != clientHiPassword)
                        {
                            // - IF AUTH FAIL
                            //      SERVER : NOT_AUTHORIZED
                            conn.Send(
                                rsaClient.Encrypt(
                                    Encoding.UTF8.GetBytes(
                                        RamDbMessages.SERVER_NOT_AUTHORIZED
                                        )));
                            conn.Dispose();
                        }
                        else
                        {
                            // - IF AUTH PASS
                            //      SERVER : OK
                            conn.Send(
                                rsaClient.Encrypt(
                                    Encoding.UTF8.GetBytes(
                                        RamDbMessages.SERVER_OK
                                        )));

                            while (true)
                            {
                                var clientProcess = Encoding.UTF8.GetString(
                                                    rsaServer.Decrypt(conn.Receive())
                                                    );

                                #region GET_VALUE
                                if (clientProcess.StartsWith(RamDbMessages.CLIENT_GET_VALUE))
                                {
                                    // - IF PROCESS TYPE IS "GET VALUE BY KEY"
                                    //      CLIENT : GET_VALUE <key>
                                    string key = "";
                                    try
                                    {
                                        key = clientProcess.Split('<')[1].Split('>')[0];
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_GET_VALUE)}.GetKey> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    string v;
                                    if (this.DbValues.TryGetValue(key, out v) && this.DbValuesExpire[key] >= DateTime.UtcNow)
                                    {
                                        // - IF DATA EXISTS
                                        //      SERVER : FOUND
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    RamDbMessages.SERVER_FOUND
                                                    )));

                                        //      SERVER : data
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    v
                                                    )));

                                        //      SERVER : END
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    RamDbMessages.SERVER_END
                                                    )));
                                    }
                                    else
                                    {
                                        // - IF DATA NOT EXISTS
                                        //      SERVER : NOT_FOUND
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    RamDbMessages.SERVER_NOT_FOUND
                                                    )));

                                        //      SERVER : END
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    RamDbMessages.SERVER_END
                                                    )));
                                    }
                                }
                                #endregion
                                #region SET_VALUE
                                else if (clientProcess.StartsWith(RamDbMessages.CLIENT_SET_VALUE))
                                {
                                    // - IF PROCESS TYPE IS "SET VALUE BY KEY"
                                    //      CLIENT : SET_VALUE <key> <time>
                                    var clientProcessSplit = clientProcess.Split('<');
                                    string key = "";
                                    DateTime time;

                                    try
                                    {
                                        key = clientProcessSplit[1].Split('>')[0];
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_SET_VALUE)}.GetKey> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    try
                                    {
                                        time = DateTime.UtcNow.Add(
                                                    TimeSpan.FromTicks(
                                                        Convert.ToInt64(
                                                            clientProcessSplit[2].Split('>')[0]
                                                            )));
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_SET_VALUE)}.GetTime> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    //      CLIENT : data
                                    var data = Encoding.UTF8.GetString(
                                                        rsaServer.Decrypt(conn.Receive())
                                                        );
                                    bool added = true;
                                    this.DbValues.AddOrUpdate(key, (o) => data, (o, x) =>
                                    {
                                        added = false;
                                        return data;
                                    });
                                    this.DbValuesExpire.AddOrUpdate(key, (o) => time, (o, x) => time);

                                    //      SERVER : ADDED / UPDATED
                                    conn.Send(
                                        rsaClient.Encrypt(
                                            Encoding.UTF8.GetBytes(
                                                added ? RamDbMessages.SERVER_ADDED : RamDbMessages.SERVER_UPDATED
                                                )));

                                    //      SERVER : END
                                    conn.Send(
                                        rsaClient.Encrypt(
                                            Encoding.UTF8.GetBytes(
                                                RamDbMessages.SERVER_END
                                                )));
                                }
                                #endregion
                                #region DELETE_VALUE
                                else if (clientProcess.StartsWith(RamDbMessages.CLIENT_DELETE_VALUE))
                                {
                                    // - IF PROCESS TYPE IS "DELETE VALUE BY KEY"
                                    //      CLIENT : DELETE_VALUE <key> <time>
                                    string key = "";

                                    try
                                    {
                                        key = clientProcess.Split('<')[1].Split('>')[0];
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_DELETE_VALUE)}.GetKey> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    string v;
                                    this.DbValues.TryRemove(key, out v);

                                    //      SERVER : END
                                    conn.Send(
                                        rsaClient.Encrypt(
                                            Encoding.UTF8.GetBytes(
                                                RamDbMessages.SERVER_END
                                                )));
                                }
                                #endregion
                                #region INCREMENT_VALUE
                                else if (clientProcess.StartsWith(RamDbMessages.CLIENT_INCREMENT_VALUE))
                                {
                                    // - IF PROCESS TYPE IS "INCREMENT VALUE BY KEY"
                                    //      CLIENT : INCREMENT_VALUE <key> <time>
                                    string key = "";

                                    try
                                    {
                                        key = clientProcess.Split('<')[1].Split('>')[0];
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_INCREMENT_VALUE)}.GetKey> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    //      CLIENT : data
                                    decimal data;

                                    try
                                    {
                                        data = Convert.ToDecimal(
                                                    Encoding.UTF8.GetString(
                                                        rsaServer.Decrypt(conn.Receive())
                                                        ), CultureInfo.InvariantCulture);
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_INCREMENT_VALUE)}.GetData> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    string lastData = "";
                                    try
                                    {
                                        this.DbValues.AddOrUpdate(key, (o) =>
                                        {
                                            lastData = data.ToString(CultureInfo.InvariantCulture);
                                            return lastData;
                                        }, (o, x) =>
                                        {
                                            lastData = (data + Convert.ToDecimal(x, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
                                            return lastData;
                                        });
                                        this.DbValuesExpire.AddOrUpdate(key, (o) => DateTime.UtcNow.AddYears(1), (o, x) => x);
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_INCREMENT_VALUE)}.SetValue> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    //      SERVER : data
                                    conn.Send(
                                        rsaClient.Encrypt(
                                            Encoding.UTF8.GetBytes(
                                                lastData
                                                )));

                                    //      SERVER : END
                                    conn.Send(
                                        rsaClient.Encrypt(
                                            Encoding.UTF8.GetBytes(
                                                RamDbMessages.SERVER_END
                                                )));
                                }
                                #endregion
                                #region DECREMENT_VALUE
                                else if (clientProcess.StartsWith(RamDbMessages.CLIENT_DECREMENT_VALUE))
                                {
                                    // - IF PROCESS TYPE IS "DECREMENT VALUE BY KEY"
                                    //      CLIENT : DECREMENT_VALUE <key> <time>
                                    string key = "";

                                    try
                                    {
                                        key = clientProcess.Split('<')[1].Split('>')[0];
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_DECREMENT_VALUE)}.GetKey> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    //      CLIENT : data
                                    decimal data;

                                    try
                                    {
                                        data = Convert.ToDecimal(
                                                    Encoding.UTF8.GetString(
                                                        rsaServer.Decrypt(conn.Receive())
                                                        ), CultureInfo.InvariantCulture);
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_DECREMENT_VALUE)}.GetData> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    string lastData = "";
                                    try
                                    {
                                        this.DbValues.AddOrUpdate(key, (o) =>
                                        {
                                            lastData = data.ToString(CultureInfo.InvariantCulture);
                                            return lastData;
                                        }, (o, x) =>
                                        {
                                            lastData = (Convert.ToDecimal(x, CultureInfo.InvariantCulture) - data).ToString(CultureInfo.InvariantCulture);
                                            return lastData;
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        conn.Send(
                                            rsaClient.Encrypt(
                                                Encoding.UTF8.GetBytes(
                                                    $"{RamDbMessages.SERVER_ERROR} <{nameof(RamDbMessages.CLIENT_DECREMENT_VALUE)}.SetValue> \"{ex.ToString().Replace("\"", "\\\"")}\""
                                                    )));
                                        continue;
                                    }

                                    //      SERVER : data
                                    conn.Send(
                                        rsaClient.Encrypt(
                                            Encoding.UTF8.GetBytes(
                                                lastData
                                                )));

                                    //      SERVER : END
                                    conn.Send(
                                        rsaClient.Encrypt(
                                            Encoding.UTF8.GetBytes(
                                                RamDbMessages.SERVER_END
                                                )));
                                }
                                #endregion
                                #region CLOSE
                                else if (clientProcess.StartsWith(RamDbMessages.CLIENT_CLOSE))
                                {
                                    // - IF PROCESS TYPE IS "CLOSE"
                                    //      CLIENT : CLOSE
                                    //      SERVER : END
                                    conn.Dispose();
                                    break;
                                }
                                #endregion
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            client.Dispose();
                        }
                        catch
                        { }

                        Console.WriteLine("A connetion get an error = " + ex.ToString());
                    }
                });
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            this._actionQueueConnections.Dispose();
            this._listener.Stop();
        }
        #endregion
    }
}
