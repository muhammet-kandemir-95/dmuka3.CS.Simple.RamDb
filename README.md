# dmuka3.CS.Simple.RamDb

 This library provides you to store data on RAM. It will never save to disk just like **Redis**.
 
 ## Nuget
 **Link** : https://www.nuget.org/packages/dmuka3.CS.Simple.RamDb
 ```nuget
 Install-Package dmuka3.CS.Simple.RamDb
 ```
 
 ## Example 1

  We should create a server to connect and to get datas from it. After that, we will connect to this server.
  
  ```csharp
  // Creating a server.
  RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
  // We have to start it on background.
  new Thread(() =>
  {
      server.Start();
  }).Start();

  // Wait for server.
  Thread.Sleep(1000);

  // Client to connect to the server.
  RamDbClient client = new RamDbClient("127.0.0.1", 9090);
  // Connected.
  client.Start("muhammed", "123123", 2048);

  // Create a key with value for 30 second.
  client.SetValue("TEST_KEY", "MUHAMMET KANDEMİR", TimeSpan.FromSeconds(30), out _);
  // Get a value by key.
  var v = client.GetValue("TEST_KEY");

  // Close client.
  client.Dispose();
  // Close server.
  server.Dispose();

  Assert.AreEqual(v, "MUHAMMET KANDEMİR");
  ```
  
  You may wonder what is the "2048". It is key size of RSA. We always use RSA during communication on TCP. This is security to save your secret datas.
  
  Also, we will show you how to lock a data on single thread.
  
## Example 2

```csharp
// Create a server.
RamDbServer server = new RamDbServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
// Run the server on background.
new Thread(() =>
{
    server.Start();
}).Start();

// Wait server.
Thread.Sleep(1000);

// Create client.
RamDbClient client = new RamDbClient("127.0.0.1", 9090);
// Connect to the server.
client.Start("muhammed", "123123", 2048);

// Our test variables.
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
                // Here is fairly important.
                // Because we are checking the queue on a single thread.
                // When we come here with double thread, first thread will pass but second will stop here and wait previous thread to unlock the queue.
                client.Lock("TEST_LOCK_KEY", TimeSpan.FromSeconds(30));

                l.Add(true);
                foreach (var item in l)
                    Thread.Sleep(1);

                // We unlock the queue for next thread.
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
```
