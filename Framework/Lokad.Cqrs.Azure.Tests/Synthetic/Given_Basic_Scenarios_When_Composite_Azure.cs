#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Feature.AtomicStorage;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Lokad.Cqrs.Synthetic
{
    [TestFixture]
    public sealed class Given_Basic_Scenarios_When_Composite_Azure : Given_Basic_Scenarios
    {
        public sealed class Handler : Define.Handle<FailingMessage>
        {
            readonly NuclearStorage _storage;

            public Handler(NuclearStorage storage)
            {
                _storage = storage;
            }

            public void Handle(FailingMessage message)
            {
                SmartFailing(message, _storage);
            }
        }

        protected override void Wire_partition_to_handler(CqrsEngineBuilder config)
        {
            // Azure dev is implemented via WS on top of SQL on top of FS.
            // this can be slow. And it will be
            TestSpeed = 7000;

            var dev = AzureStorage.CreateConfigurationForDev();
            WipeAzureAccount.Fast(s => s.StartsWith("test-"), dev);

            config.MessagesWithHandlersFromAutofac(ddd => ddd.WhereMessages(t => t.BaseType == GetType()));
            config.Azure(m =>
                {
                    m.AddAzureProcess(dev, new[] {"test-incoming"}, c =>
                        {
                            c.QueueVisibility(1);
                            c.DispatchAsCommandBatchWithAutofac();
                        });
                    m.AddAzureSender(dev, "test-incoming", x => x.IdGeneratorForTests());
                });
        }
    }
}