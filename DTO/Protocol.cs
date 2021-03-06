﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Protocol.cs" company="Don't NET">  
// 
// </copyright>
// <summary>
//   Definicja protokołu komunikacyjnego Response
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DTO
{
    using System;
    using System.Collections.Generic;

    public enum Status
    {
        OK,
        Error
    }

    public class Response
    {
        public Status Status { get; set; }
        public string Message { get; set; }

        public Response()
        {
            this.Status = DTO.Status.Error;
            this.Message = "Cos poszlo nie tak.";
        }
    }

    public class Request
    {
        public enum Type
        {
            Login,
            Register,
            AddFriend,
            RemoveFriend,
            GetFriends,
            OldMessages
        }
        public string Login { get; set; }
        public Type RequestType { get; set; }
    }

    public class Notification
    {

    }

    public class AuthRequest : Request
    {
  
        public string Password { get; set; }
    }

    public class FriendRequest : Request
    {
        public string FriendLogin { get; set; }
    }


    public class AuthResponse : Response
    {
    }

    public class FriendResponse : Response
    {
        public List<string> friendsList { get; set; }
    }

    public class UserListReq : Request
    {
    }

    public enum PresenceStatus
    {
        Online,
        Offline,
        Afk,
        Login,
        Demand
    }

    public class User
    {
        public string Login { get; set; }
        public PresenceStatus Status { get; set; }
    }

    public class UserListResponse : Response
    {
        public List<User> Users { get; set; }
    }

    public class Attachment
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }
        public string MimeType { get; set; }
    }

    public class MessageReq : Request
    {
        public string Recipient { get; set; }
        public string Message { get; set; }
        public Attachment Attachment { get; set; }
        //Server
        public DateTimeOffset SendTime { get; set; }
    }

    public class MessageResponse : Response
    {
    }


    public class MessageNotification
    {
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public string Message { get; set; }
        public Attachment Attachment { get; set; }
        public DateTimeOffset SendTime { get; set; }
    }

    public class PresenceStatusNotification : Notification
    {
        public string Login { get; set; }
        public PresenceStatus PresenceStatus { get; set; }

        public string Recipient { get; set; }
    }

    public class ActivityNotification : Notification
    {
        public string Sender { get; set; }
        public bool IsWriting { get; set; }
    }

    public class ActivityReq : Request
    {
        public bool IsWriting { get; set; }
        public string Recipient { get; set; }
    }

    public class ActivityResponse : Response
    {
    }
}
