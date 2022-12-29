using System.Net.Sockets;
using System.Net;

// https://protohackers.com/problem/0

// Constants for our echo server
const int PORT = 11000;
const int RECEIVE_BUFFER_SIZE = 8192;

// Get our IP endpoint
IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(Dns.GetHostName());
IPAddress ipAddress = ipHostInfo.AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
IPEndPoint ipEndPoint = new(ipAddress, PORT);

// Bind the socket listener to our endpoint
// Note: If behind a firewall (likely) you'll need to set up port forwarding
using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(ipEndPoint);
listener.Listen(100);

Console.WriteLine($"Started listening on {ipEndPoint}");

SocketAsyncEventArgs e = new();
e.Completed += AcceptSocketCallback;
if (!listener.AcceptAsync(e))
{
    AcceptSocketCallback(listener, e);
}

// Keep console active until keypress
Console.ReadKey(true);

static async void AcceptSocketCallback(object? sender, SocketAsyncEventArgs e)
{
    if (sender == null) return;
    Socket listenSocket = (Socket)sender;

    do
    {
        try
        {
            Socket? newSocket = e.AcceptSocket;
            if (newSocket == null) return;

            Console.WriteLine($"Connection open {newSocket.RemoteEndPoint}");

            byte[] buffer = new byte[RECEIVE_BUFFER_SIZE];
            int received;

            do
            {
                received = await newSocket.ReceiveAsync(buffer, SocketFlags.None);

                if (received > 0)
                {
                    Console.WriteLine($" << Received {received} bytes...");

                    // Echo the data back
                    await newSocket.SendAsync(buffer[..received], SocketFlags.None);
                }
            } while (received > 0);

            Console.WriteLine($"Connection closing {newSocket.RemoteEndPoint}");
            newSocket.Disconnect(false);
            newSocket.Close();
        }
        catch
        {
            // handle any exceptions here;
            Console.WriteLine("Oops an error occurred!");
        }
        finally
        {
            e.AcceptSocket = null; // to enable reuse
        }
    } while (!listenSocket.AcceptAsync(e));
}