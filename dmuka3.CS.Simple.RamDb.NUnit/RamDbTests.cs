using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace dmuka3.CS.Simple.RamDb.NUnit
{
    public class RamDbTests
    {
        [Test]
        public void ClassicTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            client.Start("muhammed", "123123", 2048);

            client.SetValue("TEST_KEY", "MUHAMMET KANDEMİR", TimeSpan.FromSeconds(30), out _);
            var v = client.GetValue("TEST_KEY");

            client.Dispose();
            server.Dispose();

            Assert.AreEqual(v, "MUHAMMET KANDEMİR");
        }

        [Test]
        public void WrongUserNameTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            bool err = false;
            try
            {
                client.Start("muhammed2", "123123", 2048);
            }
            catch (Exception ex)
            {
                err = ex.Message.StartsWith("NOT_AUTHORIZED");
            }


            client.Dispose();
            server.Dispose();

            Assert.IsTrue(err);
        }

        [Test]
        public void WrongPasswordTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            bool err = false;
            try
            {
                client.Start("muhammed", "123124", 2048);
            }
            catch (Exception ex)
            {
                err = ex.Message.StartsWith("NOT_AUTHORIZED");
            }


            client.Dispose();
            server.Dispose();

            Assert.IsTrue(err);
        }

        [Test]
        public void ExpireTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            client.Start("muhammed", "123123", 2048);

            client.SetValue("TEST_KEY", "MUHAMMET KANDEMİR", TimeSpan.FromSeconds(1), out _);

            Thread.Sleep(2000);
            var v = client.GetValue("TEST_KEY");

            client.Dispose();
            server.Dispose();

            Assert.IsNull(v);
        }

        [Test]
        public void DeleteTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            client.Start("muhammed", "123123", 2048);

            client.SetValue("TEST_KEY", "MUHAMMET KANDEMİR", TimeSpan.FromSeconds(30), out _);
            var v1 = client.GetValue("TEST_KEY");
            client.DeleteValue("TEST_KEY");
            var v2 = client.GetValue("TEST_KEY");

            client.Dispose();
            server.Dispose();

            Assert.AreEqual(v1, "MUHAMMET KANDEMİR");
            Assert.IsNull(v2);
        }

        [Test]
        public void IncrementAndDecrementTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            client.Start("muhammed", "123123", 2048);

            decimal dv = 34524542.547m;
            decimal addv = 937569475.6589m;
            decimal subtractv = 23698.5487833m;

            client.SetValue("TEST_KEY", dv.ToString(CultureInfo.InvariantCulture), TimeSpan.FromSeconds(30), out _);

            var v1 = Convert.ToDecimal(client.GetValue("TEST_KEY"), CultureInfo.InvariantCulture);

            client.IncrementValue("TEST_KEY", addv);
            var v2 = Convert.ToDecimal(client.GetValue("TEST_KEY"), CultureInfo.InvariantCulture);

            client.DecrementValue("TEST_KEY", subtractv);
            var v3 = Convert.ToDecimal(client.GetValue("TEST_KEY"), CultureInfo.InvariantCulture);

            client.Dispose();
            server.Dispose();

            Assert.AreEqual(v1, dv);
            Assert.AreEqual(v2, dv + addv);
            Assert.AreEqual(v3, dv + addv - subtractv);
        }

        [Test]
        public void LockTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            client.Start("muhammed", "123123", 2048);

            List<bool> l = new List<bool>();
            List<Thread> ts = new List<Thread>();
            bool err = false;
            for (int i = 0; i < 10; i++)
            {
                ts.Add(new Thread(() =>
                {
                    try
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            client.Lock("TEST_LOCK_KEY", TimeSpan.FromSeconds(30));

                            l.Add(true);
                            foreach (var item in l)
                                Thread.Sleep(1);

                            client.UnLock("TEST_LOCK_KEY");
                        }
                    }
                    catch (Exception ex)
                    {
                        err = true;
                    }
                }));
            }

            foreach (var item in ts)
                item.Start();

            Thread.Sleep(5000);
            client.Dispose();
            server.Dispose();

            Assert.IsFalse(err);
            Assert.AreEqual(l.Count, 50);
        }

        [Test]
        public void DataTypeTest()
        {
            RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
            new Thread(() =>
            {
                server.Start();
            }).Start();

            Thread.Sleep(1000);

            RamDbClient client = new RamDbClient("127.0.0.1", 9090);
            client.Start("muhammed", "123123", 2048);

            bool err = false;

            try
            {
                // Boolean
                client.SetValueAsBool("TEST_DATA_KEY", true, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsBool("TEST_DATA_KEY"), true);

                client.SetValueAsBool("TEST_DATA_KEY", false, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsBool("TEST_DATA_KEY"), false);

                // Byte
                client.SetValueAsByte("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsByte("TEST_DATA_KEY"), 123);

                client.SetValueAsByte("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsByte("TEST_DATA_KEY"), 97);

                // SByte
                client.SetValueAsSByte("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsSByte("TEST_DATA_KEY"), 123);

                client.SetValueAsSByte("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsSByte("TEST_DATA_KEY"), 97);

                // Int16
                client.SetValueAsInt16("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsInt16("TEST_DATA_KEY"), 123);

                client.SetValueAsInt16("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsInt16("TEST_DATA_KEY"), 97);

                // UInt16
                client.SetValueAsUInt16("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsUInt16("TEST_DATA_KEY"), 123);

                client.SetValueAsUInt16("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsUInt16("TEST_DATA_KEY"), 97);

                // Int32
                client.SetValueAsInt32("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsInt32("TEST_DATA_KEY"), 123);

                client.SetValueAsInt32("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsInt32("TEST_DATA_KEY"), 97);

                // UInt32
                client.SetValueAsUInt32("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsUInt32("TEST_DATA_KEY"), 123);

                client.SetValueAsUInt32("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsUInt32("TEST_DATA_KEY"), 97);

                // Int64
                client.SetValueAsInt64("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsInt64("TEST_DATA_KEY"), 123);

                client.SetValueAsInt64("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsInt64("TEST_DATA_KEY"), 97);

                // UInt64
                client.SetValueAsUInt64("TEST_DATA_KEY", 123, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsUInt64("TEST_DATA_KEY"), 123);

                client.SetValueAsUInt64("TEST_DATA_KEY", 97, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsUInt64("TEST_DATA_KEY"), 97);

                // Single
                client.SetValueAsSingle("TEST_DATA_KEY", 123.45f, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsSingle("TEST_DATA_KEY"), 123.45f);

                client.SetValueAsSingle("TEST_DATA_KEY", 97.3678f, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsSingle("TEST_DATA_KEY"), 97.3678f);

                // Double
                client.SetValueAsDouble("TEST_DATA_KEY", 123.45d, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsDouble("TEST_DATA_KEY"), 123.45d);

                client.SetValueAsDouble("TEST_DATA_KEY", 97.3678d, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsDouble("TEST_DATA_KEY"), 97.3678d);

                // Decimal
                client.SetValueAsDecimal("TEST_DATA_KEY", 123.45m, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsDecimal("TEST_DATA_KEY"), 123.45m);

                client.SetValueAsDecimal("TEST_DATA_KEY", 97.3678m, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsDecimal("TEST_DATA_KEY"), 97.3678m);

                // DateTime
                var dtv = DateTime.Now;
                client.SetValueAsDateTime("TEST_DATA_KEY", dtv, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsDateTime("TEST_DATA_KEY"), dtv);

                dtv = DateTime.Now.AddMinutes(40);
                client.SetValueAsDateTime("TEST_DATA_KEY", dtv, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsDateTime("TEST_DATA_KEY"), dtv);

                // TimeSpan
                var tsv = TimeSpan.FromDays(12);
                client.SetValueAsTimeSpan("TEST_DATA_KEY", tsv, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsTimeSpan("TEST_DATA_KEY"), tsv);

                tsv = TimeSpan.FromMinutes(52);
                client.SetValueAsTimeSpan("TEST_DATA_KEY", tsv, TimeSpan.FromSeconds(30), out _);
                Assert.AreEqual(client.GetValueAsTimeSpan("TEST_DATA_KEY"), tsv);
            }
            catch 
            {
                err = true;
            }

            client.Dispose();
            server.Dispose();

            Assert.IsFalse(err);
        }
    }
}