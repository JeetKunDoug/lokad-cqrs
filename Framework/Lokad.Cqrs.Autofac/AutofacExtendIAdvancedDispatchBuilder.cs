#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Feature.HandlerClasses;

namespace Lokad.Cqrs
{
    public static class AutofacExtendIAdvancedDispatchBuilder
    {
        /// <summary>
        /// <para>Wires <see cref="DispatchOneEvent"/> implementation of <see cref="ISingleThreadMessageDispatcher"/> 
        /// into this partition. It allows dispatching a single event to zero or more consumers.</para>
        /// <para> Additional information is available in project docs.</para>
        /// </summary>
        public static void DispatchAsEventsWithAutofac(this IAdvancedDispatchBuilder builder, Action<MessageDirectoryFilter> optionalFilter = null)
        {
            var action = optionalFilter ?? (x => { });
            builder.DispatcherIs(ctx => AutofacHandlerDispatchFactory.OneEvent(ctx, action));
        }

        /// <summary>
        /// <para>Wires <see cref="DispatchCommandBatch"/> implementation of <see cref="ISingleThreadMessageDispatcher"/> 
        /// into this partition. It allows dispatching multiple commands (in a single envelope) to one consumer each.</para>
        /// <para> Additional information is available in project docs.</para>
        /// </summary>
        public static void DispatchAsCommandBatchWithAutofac(this IAdvancedDispatchBuilder builder, Action<MessageDirectoryFilter> optionalFilter = null)
        {
            var action = optionalFilter ?? (x => { });
            builder.DispatcherIs(ctx => AutofacHandlerDispatchFactory.CommandBatch(ctx, action));
        }
    }
}