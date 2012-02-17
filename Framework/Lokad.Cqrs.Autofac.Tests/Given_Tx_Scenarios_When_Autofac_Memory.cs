#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Feature.AtomicStorage;
using Lokad.Cqrs.Synthetic;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Lokad.Cqrs
{
    [TestFixture]
    public sealed class Given_Tx_Scenarios_When_Autofac_Memory : Given_Tx_Scenarios
    {
        public sealed class Handler : Define.Handle<Act>
        {
            readonly NuclearStorage _storage;

            public Handler(NuclearStorage storage)
            {
                _storage = storage;
            }

            public void Handle(Act message)
            {
                Consume(message, _storage);
            }
        }

        protected override void Wire_partition_to_handler(CqrsEngineBuilder config)
        {
            config.MessagesWithHandlersFromAutofac(d => d.WhereMessagesAre<Act>());
            config.Memory(m =>
                {
                    m.AddMemorySender("do");
                    m.AddMemoryProcess("do", x => x.DispatchAsCommandBatchWithAutofac());
                });
        }
    }
}