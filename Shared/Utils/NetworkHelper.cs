using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using Telemedicina.Shared.Models;

namespace Telemedicina.Shared.Utils
{
    public static class NetworkHelper
    {
        private static readonly BinaryFormatter formatter = new BinaryFormatter();

        public static byte[] SerializeMessage(NetworkMessage message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, message);
                return ms.ToArray();
            }
        }

        public static NetworkMessage DeserializeMessage(byte[] data, int length)
        {
            using (MemoryStream ms = new MemoryStream(data, 0, length))
            {
                return (NetworkMessage)formatter.Deserialize(ms);
            }
        }

        public static void SendMessage(NetworkMessage message, Socket clientSocket)
        {
            try
            {
                byte[] data = SerializeMessage(message);
                clientSocket.Send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri slanju poruke: {ex.Message}");
            }
        }

        public static NetworkMessage ReceiveMessage(Socket clientSocket)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesReceived = clientSocket.Receive(buffer);
                
                if (bytesReceived > 0)
                {
                    return DeserializeMessage(buffer, bytesReceived);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri primanju poruke: {ex.Message}");
                return null;
            }
        }
    }
}
