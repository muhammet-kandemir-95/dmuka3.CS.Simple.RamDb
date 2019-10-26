using NUnit.Framework;
using System;
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
    }
}