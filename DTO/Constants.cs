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
        public static ConnectionFactory ConnectionFactory = new ConnectionFactory() { HostName = "localhost" }; //"10.100.6.119", UserName = "dna", Password = "dna" };
        public static string keyServerRequestMessage = "server.request.message";
        public static string keyServerRequestGetOld = "server.request.getold";
        public static string keyServerRequestLogOff = "server.request.logoff";
        public static string keyClientNotificationMessage = "client.notification.message.";
        public static string keyServerRequestStatus = "server.request.status";
        public static string keyClientNotificationStatus = "client.notification.status.";
        public static string keyServerRequestAuthorization = "server.request.authorization";
        public static string Exchange = "ClientExchange";
        public static string keyClientNotification = "client.notification";
        public static string keyRequestMessage = "server.request.message";

        public static string keyServerRequest = "server.request";
        public static string userPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}
