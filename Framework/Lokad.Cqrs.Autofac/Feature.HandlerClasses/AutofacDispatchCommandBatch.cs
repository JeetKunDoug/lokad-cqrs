#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cqrs.Core.Dispatch;

namespace Lokad.Cqrs.Feature.HandlerClasses
{
    public class AutofacDispatchCommandBatch : ISingleThreadMessageDispatcher
    {
        readonly IDictionary<Type, Type> _messageConsumers = new Dictionary<Type, Type>();
        readonly Type[] _messageDirectory;
        readonly AutofacDispatchStrategy _strategy;

        public AutofacDispatchCommandBatch(Type[] messageDirectory, AutofacDispatchStrategy strategy)
        {
            _messageDirectory = messageDirectory;
            _strategy = strategy;
        }

        void ISingleThreadMessageDispatcher.DispatchMessage(ImmutableEnvelope message)
        {
            // empty message, hm...
            if (message.Items.Length == 0)
                return;
            _strategy.Dispatch(message);
        }

        public void Init()
        {
        }

        private void ThrowIfCommandHasMultipleConsumers(Type[] commands)
        {
            var multipleConsumers = commands
                .Select(c =>
                    new
                        {
                            CommandType = c,
                            Handlers = _strategy.HandlersFor(c)
                        })
                .Where(ch => ch.Handlers.Count() > 1)
                .Select(ch => ch.CommandType);

            if (!multipleConsumers.Any())
                return;

            var joined = string.Join("; ", multipleConsumers);

            throw new InvalidOperationException(
                "These messages have multiple consumers. Did you intend to declare them as events? " + joined);
        }
    }
}