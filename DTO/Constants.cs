using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Don't NET">  
// 
// </copyright>
// <summary>
//   Definicja stałych
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DTO
{
    using RabbitMQ.Client;
    public static class Constants
    {
        public static ConnectionFactory ConnectionFactory = new ConnectionFactory() { HostName = "localhost" };//192.168.0.103", UserName = "dna", Password = "dna" };
        public static string keyServerRequestMessage = "server.request.message";
        public static string keyClientNotificationMessage = "client.notification.message.";
        public static string keyServerRequestAuthorization = "server.request.authorization";
        public static string Exchange = "ClientExchange";
        public static string keyClientNotification = "client.notification";
        public static string keyRequestMessage = "server.request.message";
    }
}
