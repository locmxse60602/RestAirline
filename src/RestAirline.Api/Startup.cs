﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using EventFlow;
using EventFlow.AspNetCore.Extensions;
using EventFlow.Autofac.Extensions;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.MsSql;
using EventFlow.MsSql.EventStores;
using EventFlow.MsSql.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestAirline.Api.Filters;
using RestAirline.Api.ServiceModules;
using RestAirline.Api.Swagger;
using RestAirline.CommandHandlers;
using RestAirline.Domain;
using RestAirline.Domain.EventSourcing;
using RestAirline.QueryHandlers;
using RestAirline.ReadModel.EntityFramework;
using RestAirline.ReadModel.InMemory;
using Swashbuckle.AspNetCore.Swagger;

namespace RestAirline.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            
            services.AddMvc(options =>
            {
                options.Filters.Add<UnhandledExceptionFilter>();
            });
            
            SwaggerServicesConfiguration.Confirure(services);

            services.AddHealthChecks();
            
            services.AddHttpContextAccessor();

            var serviceProvider = EventFlowOptions.New
                .UseServiceCollection(services)
                .AddAspNetCore(options => { options.AddUserClaimsMetadata(); })
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(Configuration["EventStoreConnectionString"]))
                .UseMssqlEventStore()
                .RegisterModule<EventStoreModule>()
                .RegisterModule<CommandModule>()
                .RegisterModule<CommandHandlersModule>()
                .RegisterModule<QueryHandlersModule>()
                .RegisterModule<BookingDomainModule>()
                .RegisterModule<InMemoryReadModelModule>()
                .RegisterModule<EntityFrameworkReadModelModule>()
                .CreateServiceProvider();

            return serviceProvider;

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
            
            app.UseHealthChecks("/health");
            
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
    }
}