﻿using Caching.Faster.Worker;
using Caching.Faster.Worker.Core;
using Caching.Faster.Workers.Core;
using FASTER.core;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Faster.Workers.Extensions
{
    public static class FasterExtensions
    {
        public static FasterKV<Key, Value, Input, Output, CacheContext, CacheFunctions> Values { get; set; }
        public static FasterKV<KeyHeader, ValueHeader, KeyHeader, ValueHeader, CacheContext, HeaderCacheFunctions> Headers { get; set; }

        public static IServiceCollection AddFaster(this IServiceCollection services)
        {
            /// Faster log for Values
            var log = Devices.CreateLogDevice("", deleteOnClose: true);
            var objlog = Devices.CreateLogDevice("", deleteOnClose: true);

            var logSettings = new LogSettings
            {
                LogDevice = log,
                ObjectLogDevice = objlog,
                ReadCacheSettings = new ReadCacheSettings()
            };

            /// Faster log for Headers
            var log_header = Devices.CreateLogDevice("", deleteOnClose: true);
            var objlog_header = Devices.CreateLogDevice("", deleteOnClose: true);

            var logSettings_header = new LogSettings
            {
                LogDevice = log_header,
                ObjectLogDevice = objlog_header,
                ReadCacheSettings = new ReadCacheSettings()                
            };

            /// Faster instance for values
            Values = new FasterKV<Key, Value, Input, Output, CacheContext, CacheFunctions>(
                    1L << 20,
                    new CacheFunctions(),
                    logSettings,
                    null,
                    new SerializerSettings<Key, Value> { keySerializer = () => new CacheKeySerializer(), valueSerializer = () => new CacheValueSerializer() }

                );

            /// Faster instance for headers
            Headers = new FasterKV<KeyHeader, ValueHeader, KeyHeader, ValueHeader, CacheContext, HeaderCacheFunctions>(
                    1L << 20,
                    new HeaderCacheFunctions(),
                    logSettings,
                    null,
                    new SerializerSettings<KeyHeader, ValueHeader> { keySerializer = () => new CacheKeyHeaderSerializer(), valueSerializer = () => new CacheValueHeaderSerializer() }

                );

            services.AddSingleton(Values);
            services.AddSingleton(Headers);

            return services;
        }

        public static IApplicationBuilder UseFasterWithGrpc(this IApplicationBuilder app)
        {
            var server = new Server
            {
                Ports = { new ServerPort("0.0.0.0", 90, ServerCredentials.Insecure) }
            };

            //faster.Log.Scan(faster.Log.BeginAddress, faster.Log.TailAddress);

            server.Services.Add(GrpcWorker.BindService(new CachingService(Values, Headers)));

            server.Start();

            return app;
        }
    }
}
