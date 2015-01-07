// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonSerializeExtensions.cs" company="">
//   
// </copyright>
// <summary>
//   Statyczna klasa zwierają metody służące do serializacji i deserializacji obiektów protokołu
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DTO
{
    using System.IO;
    using ServiceStack.Text;

    /// <summary>
    /// Statyczna klasa zwierają metody służące do serializacji i deserializacji obiektów protokołu
    /// </summary>
    public static class JsonSerializeExtensions
    {
        /// <summary>
        /// Metoda serializująca dowolny obiekt do Json'a
        /// </summary>
        /// <param name="dto">
        /// Obiekt do serializacji
        /// </param>
        /// <returns>
        /// Porcja bajtów
        /// </returns>
        public static byte[] Serialize(this object dto)
        {
            using (var stream = new MemoryStream())
            {
                JsonSerializer.SerializeToStream(dto, stream);
                return stream.GetBuffer();
            }
        }

        /// <summary>
        /// Metoda deserializacji Request'u
        /// </summary>
        /// <param name="data">
        /// Porcja bajtów
        /// </param>
        /// <returns>
        /// Zdeserializowany Request
        /// </returns>
        public static Request DeserializeRequest(this byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return JsonSerializer.DeserializeFromStream<Request>(stream);
            }
        }

        /// <summary>
        /// Metoda desrializująca request autoryzacji
        /// </summary>
        /// <param name="data">
        /// Porcja bajtów
        /// </param>
        /// <returns>
        /// Request autoryzacji
        /// </returns>
        public static AuthResponse DeserializeAuthResponse(this byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return JsonSerializer.DeserializeFromStream<AuthResponse>(stream);
            }
        }

        public static AuthRequest DeserializeAuthRequest(this byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return JsonSerializer.DeserializeFromStream<AuthRequest>(stream);
            }
        }

        public static CreateUserResponse DeserializeCreateUserResponse(this byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return JsonSerializer.DeserializeFromStream<CreateUserResponse>(stream);
            }
        }

        public static CreateUserRequest DeserializeCreateUserRequest(this byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return JsonSerializer.DeserializeFromStream<CreateUserRequest>(stream);
            }
        }

        /// <summary>
        /// Metoda deserializująca request wiadomości
        /// </summary>
        /// <param name="data">
        /// Porcja bajtów
        /// </param>
        /// <returns>
        /// Request wiadomości
        /// </returns>
        public static MessageReq DeserializeMessageReq(this byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return JsonSerializer.DeserializeFromStream<MessageReq>(stream);
            }
        }

        /// <summary>
        /// metoda deserializująca notyfikację o wiadomości
        /// </summary>
        /// <param name="data">
        /// Porcja bajtow
        /// </param>
        /// <returns>
        /// Notyfikiacja o wiadomości
        /// </returns>
        public static MessageNotification DeserializeMessageNotification(this byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return JsonSerializer.DeserializeFromStream<MessageNotification>(stream);
            }
        }
    }
}