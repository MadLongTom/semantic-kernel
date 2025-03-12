// Copyright (c) Microsoft. All rights reserved.

using AutoInvokeBufferTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.AddScoped<KernelFactory>();
builder.Services.AddHostedService<PresentationHostedService>();
IHost app = builder.Build();
app.Run();
