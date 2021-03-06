using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockMicroService.Consumers;
using StockMicroService.Models.DatabaseConfig;
using StockMicroService.Repository;
using StockMicroService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockMicroService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<StockDatabaseSettings>(
               Configuration.GetSection(nameof(StockDatabaseSettings)));

            services.AddSingleton<IStockDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<StockDatabaseSettings>>().Value);

            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IStockService, StockService>();

            var configSection = Configuration.GetSection("ServiceBus");
            var connectionUri = configSection.GetSection("ConnectionUri").Value;
            var usename = configSection.GetSection("Username").Value;
            var password = configSection.GetSection("Password").Value;
            var queueName = configSection.GetSection("QueueName").Value;
            
            services.AddMassTransit(x =>
            {
                x.AddConsumer<StockConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(connectionUri), h =>
                    {
                        h.Username(usename);
                        h.Password(password);
                    });
                    cfg.ReceiveEndpoint(queueName, ep =>
                    {
                        ep.ConfigureConsumer<StockConsumer>(context);
                    });
                });
            });
           //// services.AddMassTransitHostedService(true);
            services.AddControllers();

            services.AddMediatR(typeof(Program).Assembly);
            services.AddCors();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    });
            });

            services.AddSwaggerGen();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Stock Service API",
                    Version = "v1",
                    Description = "Stock micro services for CRUD",
                });

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger(); 
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Services"));
            
            app.UseRouting();
            app.UseAuthorization();           
         
            app.UseCors();
            app.UseCors(builder =>
            {
                builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });            
        }
    }
}
