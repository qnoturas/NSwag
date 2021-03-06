﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NJsonSchema;
using NSwag.AspNetCore;
using NSwag.SwaggerGeneration;
using NSwag.SwaggerGeneration.AspNetCore;
using NSwag.SwaggerGeneration.Processors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>NSwag extensions for <see cref="IServiceCollection"/>.</summary>
    public static class NSwagServiceCollectionExtensions
    {
        /// <summary>Adds services required for OpenAPI 3.0 generation (change document settings to generate Swagger 2.0).</summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">Configure the document.</param>
        public static IServiceCollection AddOpenApiDocument(this IServiceCollection serviceCollection, Action<SwaggerDocumentSettings> configure)
        {
            return AddOpenApiDocument(serviceCollection, (settings, services) =>
            {
                configure?.Invoke(settings);
            });
        }

        /// <summary>Adds services required for Swagger 2.0 generation (change document settings to generate OpenAPI 3.0).</summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">Configure the document.</param>
        public static IServiceCollection AddOpenApiDocument(this IServiceCollection serviceCollection, Action<SwaggerDocumentSettings, IServiceProvider> configure = null)
        {
            return AddSwaggerDocument(serviceCollection, (settings, services) =>
            {
                settings.SchemaType = SchemaType.OpenApi3;
                configure?.Invoke(settings, services);
            });
        }

        /// <summary>Adds services required for Swagger 2.0 generation (change document settings to generate OpenAPI 3.0).</summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">Configure the document.</param>
        public static IServiceCollection AddSwaggerDocument(this IServiceCollection serviceCollection, Action<SwaggerDocumentSettings> configure)
        {
            return AddSwaggerDocument(serviceCollection, (settings, services) =>
            {
                configure?.Invoke(settings);
            });
        }

        /// <summary>Adds services required for Swagger 2.0 generation (change document settings to generate OpenAPI 3.0).</summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">Configure the document.</param>
        public static IServiceCollection AddSwaggerDocument(this IServiceCollection serviceCollection, Action<SwaggerDocumentSettings, IServiceProvider> configure = null)
        {
            serviceCollection.AddSingleton(services =>
            {
                var settings = new SwaggerDocumentSettings();
                settings.SchemaType = SchemaType.Swagger2;

                configure?.Invoke(settings, services);

                if (settings.PostProcess != null)
                {
                    var processor = new ActionDocumentProcessor(context => settings.PostProcess(context.Document));
                    settings.DocumentProcessors.Add(processor);
                }

                foreach (var documentProcessor in services.GetRequiredService<IEnumerable<IDocumentProcessor>>())
                {
                    settings.DocumentProcessors.Add(documentProcessor);
                }

                foreach (var operationProcessor in services.GetRequiredService<IEnumerable<IOperationProcessor>>())
                {
                    settings.OperationProcessors.Add(operationProcessor);
                }

                var schemaGenerator = settings.SchemaGenerator ?? new SwaggerJsonSchemaGenerator(settings);
                var generator = new AspNetCoreToSwaggerGenerator(settings, schemaGenerator);

                return new SwaggerDocumentRegistration(settings.DocumentName, generator);
            });

            var descriptor = serviceCollection.SingleOrDefault(d => d.ServiceType == typeof(SwaggerDocumentProvider));
            if (descriptor == null)
            {
                serviceCollection.AddSingleton<SwaggerDocumentProvider>();
                serviceCollection.AddSingleton<IConfigureOptions<MvcOptions>, SwaggerConfigureMvcOptions>();

                // Used by UseDocumentProvider CLI setting
                serviceCollection.AddSingleton<ISwaggerDocumentProvider>(s => s.GetRequiredService<SwaggerDocumentProvider>());

                // Used by the Microsoft.Extensions.ApiDescription tool
                serviceCollection.AddSingleton<ApiDescription.IDocumentProvider>(s => s.GetRequiredService<SwaggerDocumentProvider>());
            }

            return serviceCollection;
        }

        /// <summary>Adds services required for Swagger 2.0 generation (change document settings to generate OpenAPI 3.0).</summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">Configure the document.</param>
        [Obsolete("Use " + nameof(AddSwaggerDocument) + "() instead.")]
        public static IServiceCollection AddSwagger(this IServiceCollection serviceCollection, Action<SwaggerDocumentSettings> configure = null)
        {
            return AddSwaggerDocument(serviceCollection, configure);
        }
    }
}
