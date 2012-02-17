#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System;
using System.Linq;
using Lokad.Cqrs.Core;
using Lokad.Cqrs.Core.Dispatch;

namespace Lokad.Cqrs.Feature.HandlerClasses
{
    public static class AutofacHandlerDispatchFactory
    {
        public static ISingleThreadMessageDispatcher CommandBatch(Container ctx, Action<MessageDirectoryFilter> optionalFilter)
        {
            var builder = ctx.Resolve<MessageDirectoryBuilder>();
            var filter = new MessageDirectoryFilter();
            optionalFilter(filter);

            var map = builder.BuildActivationMap(filter.DoesPassFilter);

            var messageTypes = map.Select(m => m.MessageType);

            var strategy = ctx.Resolve<AutofacDispatchStrategy>();
            return new AutofacDispatchCommandBatch(messageTypes.ToArray(), strategy);
        }
        public static ISingleThreadMessageDispatcher OneEvent(Container ctx, Action<MessageDirectoryFilter> optionalFilter)
        {
            var builder = ctx.Resolve<MessageDirectoryBuilder>();
            var filter = new MessageDirectoryFilter();
            optionalFilter(filter);

            var map = builder.BuildActivationMap(filter.DoesPassFilter);

            var strategy = ctx.Resolve<AutofacDispatchStrategy>();
            var observer = ctx.Resolve<ISystemObserver>();
            return new AutofacDispatchOneEvent(map, observer, strategy);
        }
    }
}