﻿#region (c) 2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

namespace Lokad.Cqrs.Feature.Http
{
    public interface IHttpContext
    {
        IHttpEnvironment Environment { get; }
        IHttpRequest Request { get; }
        IHttpResponse Response { get; }
        void TryCloseResponse();
    }
}