#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Lokad.Cqrs.Feature.AzurePartition
{
    public static class QueueExtensions
    {
        /// <summary>
        /// Add a message to the queue. The visibility timeout param can be used to optionally 
        /// make the message visible at a future time
        /// </summary>
        /// <param name="queue">
        /// The queue to add message to
        /// </param>
        /// <param name="credentials">
        /// The storage credentials used for signing
        /// </param>
        /// <param name="message">
        /// The message content
        /// </param>
        /// <param name="visibilityTimeout">
        /// value in seconds and should be greater than or equal to 0 and less than 604800 (7 days). 
        /// It should also be less than messageTimeToLive
        /// </param>
        /// <param name="messageTimeToLive">
        /// (Optional) Time after which the message expires if it is not deleted from the queue.
        /// It can be a maximum time of 7 days.
        /// </param>
        /// <param name="timeout">
        /// Server timeout value
        /// </param>
        public static void PutMessage(
            this CloudQueue queue,
            StorageCredentials credentials,
            CloudQueueMessage message,
            int? visibilityTimeout,
            int? messageTimeToLive,
            int timeout)
        {
            StringBuilder builder = new StringBuilder(queue.Uri.AbsoluteUri);

            builder.AppendFormat("/messages?timeout={0}", timeout);

            if (messageTimeToLive != null)
            {
                builder.AppendFormat("&messagettl={0}", messageTimeToLive.ToString());
            }

            if (visibilityTimeout != null)
            {
                builder.AppendFormat("&visibilitytimeout={0}", visibilityTimeout);
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(builder.ToString());
            request.Method = "POST";
            request.Headers.Add("x-ms-version", "2011-08-18");

            byte[] messageBytes = message.AsBytes;
            request.ContentLength = messageBytes.Length;
            credentials.SignRequest(request);
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(messageBytes, 0, messageBytes.Length);
            }

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // we expect 201 for Put Message
                    if (response.StatusCode != HttpStatusCode.Created)
                    {
                        throw new InvalidOperationException("Unexpected response code.");
                    }
                }
            }
            catch (WebException e)
            {
                // Log any exceptions for debugging
                LogWebException(e);
                throw;
            }
        }

        /// <summary>
        /// Update the message to extend visibility timeout and optionally 
        /// the message contents 
        /// </summary>
        /// <param name="queue">
        /// The queue to operate on
        /// </param>
        /// <param name="credentials">
        /// The storage credentials used for signing
        /// </param>
        /// <param name="messageId">
        /// The ID of message to extend the lease on
        /// </param>
        /// <param name="popReceipt">
        /// pop receipt to use
        /// </param>
        /// <param name="visibilityTimeout">
        /// Value should be greater than or equal to 0 and less than 7. 
        /// </param>
        /// <param name="messageBody">
        /// (optional) The message content
        /// </param>
        /// <param name="timeout">
        /// Server timeout value
        /// </param>
        /// <param name="newPopReceiptID">
        /// Return the new pop receipt that should be used for subsequent requests when 
        /// the lease is held
        /// </param>
        /// <param name="nextVisibilityTime">
        /// Return the next visibility time for the message. This is time until which the lease is held
        /// </param>
        public static void UpdateMessage(
            this CloudQueue queue,
            StorageCredentials credentials,
            string messageId,
            string popReceipt,
            int visibilityTimeout,
            string messageBody,
            int timeout,
            out string newPopReceiptID,
            out DateTime nextVisibilityTime)
        {
            StringBuilder builder = new StringBuilder(queue.Uri.AbsoluteUri);

            builder.AppendFormat(
                "/messages/{0}?timeout={1}&popreceipt={2}&visibilitytimeout={3}",
                messageId,
                timeout,
                Uri.EscapeDataString(popReceipt),
                visibilityTimeout);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(builder.ToString());
            request.Method = "PUT";
            request.Headers.Add("x-ms-version", "2011-08-18");

            if (messageBody != null)
            {
                byte[] buffer = QueueRequest.GenerateMessageRequestBody(messageBody);

                request.ContentLength = buffer.Length;
                credentials.SignRequest(request);
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            else
            {
                request.ContentLength = 0;
                credentials.SignRequest(request);
            }

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        throw new InvalidOperationException("Unexpected response code.");
                    }

                    newPopReceiptID = response.Headers["x-ms-popreceipt"];
                    nextVisibilityTime = DateTime.Parse(response.Headers["x-ms-time-next-visible"]);
                }
            }
            catch (WebException e)
            {
                // Log any exceptions for debugging
                LogWebException(e);
                throw;
            }
        }


        /// <summary>
        /// Get messages has been provided only because storage client library does not allow 
        /// invisibility timeout to exceed 2 hours
        /// </summary>
        /// <param name="queue">
        /// The queue to operate on
        /// </param>
        /// <param name="credentials">
        /// The storage credentials used for signing
        /// </param>
        /// <param name="messageId">
        /// The ID of message to extend the lease on
        /// </param>
        /// <param name="popReceipt">
        /// pop receipt to use
        /// </param>
        /// <param name="visibilityTimeout">
        /// Value should be greater than or equal to 0 and less than 7. 
        /// </param>
        /// <param name="messageBody">
        /// (optional) The message content
        /// </param>
        /// <param name="timeout">
        /// Server timeout value
        /// </param>
        /// <param name="newPopReceiptID">
        /// Return the new pop receipt that should be used for subsequent requests when 
        /// the lease is held
        /// </param>
        /// <param name="nextVisibilityTime">
        /// Return the next visibility time for the message. This is time until which the lease is held
        /// </param>
        public static IEnumerable<QueueMessage> GetMessages(
            this CloudQueue queue,
            StorageCredentials credentials,
            int? visibilityTimeout,
            int? messageCount,
            int timeout)
        {
            StringBuilder builder = new StringBuilder(queue.Uri.AbsoluteUri);

            builder.AppendFormat(
                "/messages?timeout={0}",
                timeout);

            if (messageCount != null)
            {
                builder.AppendFormat("&numofmessages={0}", messageCount);
            }

            if (visibilityTimeout != null)
            {
                builder.AppendFormat("&visibilitytimeout={0}", visibilityTimeout);
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(builder.ToString());
            request.Method = "GET";
            request.Headers.Add("x-ms-version", "2011-08-18");
            credentials.SignRequest(request);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new InvalidOperationException("Unexpected response code.");
                    }

                    GetMessagesResponse msgResponses = QueueResponse.GetMessages(response);

                    // force it to be parsed right away else the response will be closed
                    // since QueueResponse.GetMessages parses responses lazily. 
                    QueueMessage[] messages = msgResponses.Messages.ToArray<QueueMessage>();
                    return messages.AsEnumerable<QueueMessage>();
                }
            }
            catch (WebException e)
            {
                // Log any exceptions for debugging
                LogWebException(e);
                throw;
            }
        }

        /// <summary>
        /// Log the exception in your preferred logging system
        /// </summary>
        /// <param name="e">
        /// The exception to log
        /// </param>
        private static void LogWebException(WebException e)
        {
            HttpWebResponse response = e.Response as HttpWebResponse;
            Console.WriteLine(string.Format(
                "Request failed with '{0}'. Status={1} RequestId={2} Exception={3}",
                e.Message,
                response.StatusCode,
                response != null ? response.Headers["x-ms-request-id"] : "<NULL>",
                e.ToString()));

            // Log to your favorite location…

        }
    }
}