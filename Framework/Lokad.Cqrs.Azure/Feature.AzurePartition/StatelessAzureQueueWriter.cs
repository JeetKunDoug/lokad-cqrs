﻿#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using Lokad.Cqrs.Core.Outbox;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cqrs.Feature.AzurePartition
{
    public sealed class StatelessAzureQueueWriter : IQueueWriter
    {
        public string Name { get; private set; }
        public void PutMessage(ImmutableEnvelope envelope)
        {
            var packed = PrepareCloudMessage(envelope);
            var now = DateTime.UtcNow;
            TimeSpan? ttl = null;
            if (packed.ExpirationTime.HasValue)
            {
                ttl = packed.ExpirationTime.Value.Subtract(now);
            }
            TimeSpan? visibilityTimeout = envelope.DeliverOnUtc.Subtract(now);
            if (visibilityTimeout < TimeSpan.FromSeconds(0))
            {
                visibilityTimeout = null;
            }
            _queue.AddMessage(packed, ttl, visibilityTimeout);
        }

        // New azure limit is 64k after BASE 64 conversion. We Are adding 152 on top just to be safe
        const int CloudQueueLimit = 49000;


        CloudQueueMessage PrepareCloudMessage(ImmutableEnvelope builder)
        {
            var buffer = _streamer.SaveEnvelopeData(builder);
            if (buffer.Length < CloudQueueLimit)
            {
                // write message to queue
                return new CloudQueueMessage(buffer);
            }
            // ok, we didn't fit, so create reference message
            var referenceId = DateTimeOffset.UtcNow.ToString(DateFormatInBlobName) + "-" + builder.EnvelopeId;
            _cloudBlob.GetBlobReference(referenceId).UploadByteArray(buffer);
            var reference = new EnvelopeReference(builder.EnvelopeId, _cloudBlob.Uri.ToString(), referenceId);
            var blob = _streamer.SaveEnvelopeReference(reference);
            return new CloudQueueMessage(blob);
        }

        public StatelessAzureQueueWriter(StorageCredentials credentials, IEnvelopeStreamer streamer, CloudBlobContainer container, CloudQueue queue, string name)
        {
            _credentials = credentials;
            _streamer = streamer;
            _cloudBlob = container;
            _queue = queue;
            Name = name;
        }

        public void Init()
        {
            _queue.CreateIfNotExist();
            _cloudBlob.CreateIfNotExist();
        }


        const string DateFormatInBlobName = "yyyy-MM-dd-HH-mm-ss-ffff";
        readonly StorageCredentials _credentials;
        readonly IEnvelopeStreamer _streamer;
        readonly CloudBlobContainer _cloudBlob;
        readonly CloudQueue _queue;
    }
}