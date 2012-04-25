#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Linq;
using Autofac;
using Autofac.Core;
using Container = Lokad.Cqrs.Core.Container;

namespace Lokad.Cqrs.Feature.HandlerClasses
{
    /// <summary>
    /// Static class capable of building nested container provider
    /// </summary>
    public static class AutofacContainerProvider
    {
        public static IContainerForHandlerClasses Build(Container container, Type[] handlerTypes, ContainerBuilder builder)
        {
            foreach (var handlerType in handlerTypes)
            {
                builder.RegisterType(handlerType).As(
                    handlerType.GetInterfaces().
                    Where(i => i.IsClosedTypeOf(typeof(IHandle<>)))
                    .Select(i => new KeyedService("implementation", i))
                    .Cast<Service>().ToArray());
            }
            // allow handlers to resolve items from the core container
            builder.RegisterSource(new FunqAdapterForAutofac(container));
            var autofac = builder.Build();
            container.Register(autofac);
            // dispose container, when server shuts down
            container.TrackDisposable(autofac);
            return new AutofacContainerForHandlerClasses(autofac);
        }
    }
}