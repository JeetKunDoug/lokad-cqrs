#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Core.Dispatch.Events;

namespace Lokad.Cqrs.Feature.HandlerClasses
{
    public sealed class AutofacDispatchOneEvent : ISingleThreadMessageDispatcher
    {
        readonly MessageActivationInfo[] _directory;
        readonly IDictionary<Type, Type[]> _dispatcher = new Dictionary<Type, Type[]>();
        readonly ISystemObserver _observer;


        readonly AutofacDispatchStrategy _strategy;

        public AutofacDispatchOneEvent(
            MessageActivationInfo[] directory,
            ISystemObserver observer,
            AutofacDispatchStrategy strategy)
        {
            _observer = observer;
            _directory = directory;
            _strategy = strategy;
        }


        public void Init()
        {
            foreach (var message in _directory)
            {
                if (message.AllConsumers.Length > 0)
                {
                    _dispatcher.Add(message.MessageType, message.AllConsumers);
                }
            }
        }

        public void DispatchMessage(ImmutableEnvelope envelope)
        {
            if (envelope.Items.Length != 1)
                throw new InvalidOperationException(
                    "Batch message arrived to the shared scope. Are you batching events or dispatching commands to shared scope?");

            // we get failure if one of the subscribers fails
            // hence, if any of the handlers fail - we give up
            var item = envelope.Items[0];
            Type[] consumerTypes;

            if (!_dispatcher.TryGetValue(item.MappedType, out consumerTypes))
            {
                // else -> we don't have consumers. It's OK for the event
                _observer.Notify(new EventHadNoConsumers(envelope.EnvelopeId, item.MappedType));
                return;
            }

            _strategy.Dispatch(envelope);
        }
    }
}