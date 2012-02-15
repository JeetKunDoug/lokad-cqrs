﻿#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cqrs.AtomicStorage
{
    /// <summary>
    /// Basic usability wrapper for the atomic storage operations, that does not enforce concurrency handling. 
    /// If you want to work with advanced functionality, either request specific interfaces from the container 
    /// or go through the advanced members on this instance. 
    /// </summary>
    public sealed class NuclearStorage : HideObjectMembersFromIntelliSense
    {
        public readonly IAtomicContainer Factory;

        public void Reset()
        {
            Factory.Reset();
        }

        public void CopyFrom(NuclearStorage source, int degreeOfParallelism = 1)
        {
            if (Factory.Strategy != source.Factory.Strategy)
                throw new InvalidOperationException("Copying is allowed only if source has same strategy instance. " +
                                                    "Enumerate and write factory contents to override this behavior.");

            Factory.WriteContents(source.Factory.EnumerateContents());
        }




        public NuclearStorage(IAtomicContainer factory)
        {
            Factory = factory;
        }

        public bool TryDeleteEntity<TEntity>(object key)
        {
            return Factory.GetEntityWriter<object, TEntity>().TryDelete(key);
        }

        public bool TryDeleteSingleton<TEntity>()
        {
            return Factory.GetEntityWriter<unit,TEntity>().TryDelete(unit.it);
        }

        public TEntity UpdateEntity<TEntity>(object key, Action<TEntity> update)
        {
            return Factory.GetEntityWriter<object, TEntity>().UpdateOrThrow(key, update);
        }


        public TSingleton UpdateSingletonOrThrow<TSingleton>(Action<TSingleton> update)
        {
            return Factory.GetEntityWriter<unit,TSingleton>().UpdateOrThrow(unit.it, update);
        }


        public Optional<TEntity> GetEntity<TEntity>(object key)
        {
            return Factory.GetEntityReader<object, TEntity>().Get(key);
        }

        public bool TryGetEntity<TEntity>(object key, out TEntity entity)
        {
            return Factory.GetEntityReader<object, TEntity>().TryGet(key, out entity);
        }

        public TEntity AddOrUpdateEntity<TEntity>(object key, TEntity entity)
        {
            return Factory.GetEntityWriter<object, TEntity>().AddOrUpdate(key, () => entity, source => entity);
        }

        public TEntity AddOrUpdateEntity<TEntity>(object key, Func<TEntity> addFactory, Action<TEntity> update)
        {
            return Factory.GetEntityWriter<object, TEntity>().AddOrUpdate(key, addFactory, update);
        }

        public TEntity AddOrUpdateEntity<TEntity>(object key, Func<TEntity> addFactory, Func<TEntity,TEntity> update)
        {
            return Factory.GetEntityWriter<object, TEntity>().AddOrUpdate(key, addFactory, update);
        }

        public TEntity AddEntity<TEntity>(object key, TEntity newEntity)
        {
            return Factory.GetEntityWriter<object, TEntity>().Add(key, newEntity);
        }

        public TSingleton AddOrUpdateSingleton<TSingleton>(Func<TSingleton> addFactory, Action<TSingleton> update)
        {
            return Factory.GetEntityWriter<unit,TSingleton>().AddOrUpdate(unit.it, addFactory, update);
        }

        public TSingleton AddOrUpdateSingleton<TSingleton>(Func<TSingleton> addFactory,
            Func<TSingleton, TSingleton> update)
        {
            return Factory.GetEntityWriter<unit,TSingleton>().AddOrUpdate(unit.it, addFactory, update);
        }

        public TSingleton UpdateSingletonEnforcingNew<TSingleton>(Action<TSingleton> update) where TSingleton : new()
        {
            return Factory.GetEntityWriter<unit, TSingleton>().UpdateEnforcingNew(unit.it, update);
        }

        public TSingleton GetSingletonOrNew<TSingleton>() where TSingleton : new()
        {
            return Factory.GetEntityReader<unit,TSingleton>().GetOrNew();
        }

        public Optional<TSingleton> GetSingleton<TSingleton>()
        {
            return Factory.GetEntityReader<unit,TSingleton>().Get();
        }
    }
}