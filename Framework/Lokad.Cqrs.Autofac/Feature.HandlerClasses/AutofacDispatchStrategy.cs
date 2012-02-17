#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lokad.Cqrs.Evil;

namespace Lokad.Cqrs.Feature.HandlerClasses
{
    public class AutofacDispatchStrategy
   {
        readonly IContainerForHandlerClasses _scope;
        readonly HandlerClassTransactionFactory _scopeFactory;
        readonly Func<Type, Type, MethodInfo> _hint;
        readonly IMethodContextManager _context;

        public AutofacDispatchStrategy(IContainerForHandlerClasses scope, HandlerClassTransactionFactory scopeFactory,
            Func<Type, Type, MethodInfo> hint, IMethodContextManager context)
        {
            _scope = scope;
            _scopeFactory = scopeFactory;
            _hint = hint;
            _context = context;
        }

        public void Dispatch(ImmutableEnvelope envelope)
        {
            using (var tx = _scopeFactory(envelope))
            using (var envelopeScope = _scope.GetChildContainer(ContainerScopeLevel.Envelope))
            {
                foreach (var message in envelope.Items)
                {
                    using (var itemScope = envelopeScope.GetChildContainer(ContainerScopeLevel.Item))
                    {
                        // TODO: Use handler hint to get generic type
                        Type handlerType = typeof(IHandle<>).MakeGenericType(message.Content.GetType());
                        object[] handlerInstances;
                        try
                        {
                            handlerType = typeof(IHandle<>).MakeGenericType(message.Content.GetType());
                            handlerInstances = itemScope.ResolveHandlersByServiceType(handlerType);
                        }
                        catch(Exception ex)
                        {
                            var msg = string.Format("Failed to resolve handler(s) {0} from {1}. ", handlerType,
                                itemScope.GetType().Name);
                            throw new InvalidOperationException(msg, ex);
                        }
                        
                        var consume = _hint(handlerType, message.MappedType);
                        try
                        {
                            _context.SetContext(envelope, message);
                            foreach (var handlerInstance in handlerInstances)
                            {
                                consume.Invoke(handlerInstance, new[]
                                    {
                                        message.Content
                                    });
                            }
                        }
                        catch (TargetInvocationException e)
                        {
                            throw InvocationUtil.Inner(e);
                        }
                        finally
                        {
                            _context.ClearContext();
                        }
                    }
                }
                tx.Complete();
            }
        }

        public object[] HandlersFor(Type messageType)
        {
            Type handlerType = typeof(IHandle<>).MakeGenericType(messageType);
            var handlers = _scope.ResolveHandlersByServiceType(handlerType);
            return handlers;
        }
   }
}