// Program to proxy requests from a local client and corresponding responses from a remote server.
// Can view the messages or modify them as desired before passing them on.

using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace ToolkitProxy
{
    class Startup
    {
        public static void Main()
        {
            int port = Int32.Parse(ConfigurationManager.AppSettings.Get("port"));
            string host = ConfigurationManager.AppSettings.Get("hostUrl");
            ProxyServer myProxy = new(port, host);
            myProxy.StartListen().Wait();
        }
    }

    class ProxyServer
    {
        private readonly TcpListener myListener;
        private readonly int port;
        private readonly string hostUrl;
        readonly IPAddress localAddr = IPAddress.Loopback;
        readonly UTF8Encoding utf8 = new();
        const int maxRequestLen = 1024;

        public ProxyServer(int myPort, string myHost)
        {
            try
            {
                port = myPort;
                hostUrl = myHost;
                myListener = new TcpListener(localAddr, port);
                myListener.Start();
                Console.WriteLine("Web Server Running... Press ^C to Stop...");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
        }

        // Main loop for listening for requests, adjusting them, forwarding them, adjusting responses
        // and returning responses.
        public async Task StartListen()
        {  
            HttpClient client = new();
            while (true)  
            {  
                //Accept a new connection  
                Socket mySocket = myListener.AcceptSocket();  
                Console.WriteLine("Socket Type " + mySocket.SocketType);  
                if (mySocket.Connected)  
                {  
                    Console.WriteLine($"\nClient Connected!!\n==================\n CLient IP {mySocket.RemoteEndPoint}\n") ;  
                    //Make a byte array and receive data from the client   
                    Byte[] bReceive = new Byte[maxRequestLen];
                    mySocket.Receive(bReceive, bReceive.Length, 0);  
                    //Convert Byte to String  
                    string sBuffer = Encoding.ASCII.GetString(bReceive);  
                    Log($"Received: {sBuffer}");
                    try
                    {
                        HttpRequestMessage scRequest = GetAdjustedRequest(sBuffer);
                        Log($"Adjusted Request: {scRequest}");

                        HttpResponseMessage response = await client.SendAsync(scRequest);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var responseHeaders = response.Headers;
                        Log($"responseBody = {responseBody}");
                        Log($"responseHeaders = {responseHeaders}");
                        Log("=====================================================");

                        string tkResponse = await GetAdjustedResponse(response);
                        //SendFakeResponse(ref mySocket);
                        SendToClient(tkResponse, ref mySocket);
                    }
                    catch(HttpRequestException e)
                    {
                        Log($"\nHttpRequestException caught: {e.Message}");
                    }
                    catch(Exception e)
                    {
                        Log($"\nException caught: {e.Message}");
                    }

                    mySocket.Close();  
                    Log("Closed socket.");
                }  
            }
        }

        private static void Log(String logMsg)
        {
            Console.WriteLine(logMsg);
        }

        // Adjust request as desired. Currently, replaces action with get, removes all headers,
        // puts in configured host.
        private HttpRequestMessage GetAdjustedRequest(string sBuffer)
        {
            // Remove verb - GET, POST, etc.
            string uri = sBuffer[(sBuffer.IndexOf(" ") + 1) .. ^0];

            // Remove everything after uri.
            uri = uri[0 .. uri.IndexOf(" ")];
            Log($"uri: {uri}");
            var myRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            var myHeaders = myRequest.Headers;
            myHeaders.Add("Host", hostUrl);
            //myHeaders.Add("User-Agent", "IP*Works! HTTP Component - www.dev-soft.com");

            return myRequest;
        }
        
        // Make any desired changes to the response. Currently just puts an extra cr/lf after the headers
        private static async Task<string> GetAdjustedResponse(HttpResponseMessage msg)
        {
            string tkResponse = $"HTTP/{msg.Version} {(int) msg.StatusCode} {msg.StatusCode}\r\n";
            tkResponse += msg.Content.Headers;
            tkResponse += msg.Headers + "\r\n";
            tkResponse += await msg.Content.ReadAsStringAsync() + "\r\n";

            return tkResponse;
        }

        // Encode and return given string to client.
        private void SendToClient(String sData, ref Socket mySocket)  
        {  
            SendToClient(utf8.GetBytes(sData), ref mySocket);  
            Log($"SendToClient, sData = {sData}");

        }

        // Return given data to client.
        private static void SendToClient(Byte[] bSendData, ref Socket mySocket)  
        {  
            int numBytes;
            try  
            {  
                if (mySocket.Connected)  
                {  
                    if ((numBytes = mySocket.Send(bSendData, bSendData.Length, 0)) == -1)  
                        Console.WriteLine("Socket Error cannot Send Packet");  
                    else  
                    {  
                        Console.WriteLine("No. of bytes sent {0}", numBytes);  
                    }  
                }  
                else Console.WriteLine("Connection Dropped....");  
            }  
            catch (Exception e)  
            {  
                Console.WriteLine("Error Occurred : {0} ", e);  
            }  
        }  

        // Create and send hardcoded header fields to client.
        private void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, string contentDisp, ref Socket mySocket)  
        {  
            String sBuffer = GetHeader(sHttpVersion, sMIMEHeader, iTotBytes, sStatusCode, contentDisp);  
            SendToClient(sBuffer, ref mySocket);
            Console.WriteLine("Total Bytes : " + iTotBytes.ToString());  
        }  

        // Create header with given values and a bunch of hard-coded values.
        private static String GetHeader(string sHttpVersion, string sMIMEHeader, 
                                        int iTotBytes, string sStatusCode, string contentDisp)  
        {  
            String sBuffer = "";  
            // if Mime type is not provided set default to text/html  
            if (sMIMEHeader.Length == 0)  
            {  
                sMIMEHeader = "text/html";// Default Mime Type is text/html  
            }  
            sBuffer += sHttpVersion + sStatusCode + "\r\n";  
            sBuffer += "Server: cx1192619-b\r\n";  
            sBuffer += "Content-Type: " + sMIMEHeader + "\r\n";  
            sBuffer += "Content-Disposition: " + contentDisp + "\r\n";  
            sBuffer += "Transfer-Encoding: " + "chunked" + "\r\n";  
            sBuffer += "Content-Length: " + iTotBytes + "\r\n\r\n";  
            return sBuffer;
        }  
        
        // A hardcoded response used for testing the client.
        private void SendFakeResponse(ref Socket mySocket)
        {
            string response = "HTTP/1.1 200 OK\r\n";
            response += "Cache-Control: private\r\n";
            response += "Content-Type: text/csv\r\n";
            response += "Server: Microsoft-IIS/10.0\r\n";
            response += "Set-Cookie: ASP.NET_SessionId=zdyu43bnoghdt2rekloo1vql3; path=/; HttpOnly; SameSite=Lax\r\n";
            response += "Content-Disposition: attachment; filename=\"quotes.csv\"\r\n";
            response += "X-AspNet-Version: 4.0.30319\r\n";
            response += "X-Powered-By: ASP.NET\r\n";
            response += "Date: Fri, 15 Oct 2021 00:42:13 GMT\r\n";
            response += "Transfer-Encoding: chunked\r\n";
            response += "\r\n";
            response += "\"SBUX\",110.76,\"10/13/2021\",\"1:00pm\",+0.01,110.76,110.76,110.76,1,85.45,126.32";
            response += "\r\n";
            SendToClient(response, ref mySocket);
        }

    }
}
