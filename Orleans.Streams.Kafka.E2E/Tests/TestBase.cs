﻿using Microsoft.Extensions.Configuration;
using Orleans.Hosting;
using Orleans.Streams.Kafka.Config;
using Orleans.Streams.Kafka.E2E.Grains;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Streams.Kafka.E2E.Tests
{
	public class TestBase : IAsyncLifetime
	{
		private short _noOfSilos;
		protected TestCluster Cluster { get; private set; }

		protected void Initialize(short noOfSilos)
		{
			_noOfSilos = noOfSilos;
		}

		protected void ShutDown()
			=> Cluster?.StopAllSilos();

		public Task InitializeAsync()
		{
			Environment.SetEnvironmentVariable("userName", "CC4ZKCWQZHAHJASA");
			Environment.SetEnvironmentVariable("password", "OsacTSG5OtU+l6Da77MGZVqr3qrDcBlgGZZUd9Up8Vh+Z8jmhgZgOqyWrWTTHpQ/");
			
			var builder = new TestClusterBuilder(_noOfSilos);

			builder.AddSiloBuilderConfigurator<SiloBuilderConfigurator>();
			builder.AddClientBuilderConfigurator<ClientBuilderConfigurator>();

			Cluster = builder.Build();
			Cluster.Deploy();

			return Task.CompletedTask;
		}

		public Task DisposeAsync()
		{
			ShutDown();
			return Task.CompletedTask;
		}
	}

	public class ClientBuilderConfigurator : IClientBuilderConfigurator
	{
		public virtual void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
			=> clientBuilder
				.AddKafkaStreamProvider(Consts.KafkaStreamProvider, options =>
				{
					options.BrokerList = new List<string> { "pkc-l9pve.eu-west-1.aws.confluent.cloud:9092" };
					options.ConsumerGroupId = "TestGroup";
					options.Topics = new List<string> { Consts.StreamNamespace, Consts.StreamNamespace2 };
					options.PollTimeout = TimeSpan.FromMilliseconds(10);
					options.ExternalMessageIdentifier = "x-external-message";
					options.ConsumeMode = ConsumeMode.StreamEnd;
					options.WithSaslOptions(new Credentials
					{
						UserName = Environment.GetEnvironmentVariable("userName"),
						Password = Environment.GetEnvironmentVariable("password"),
						SslCaLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cacert.pem")
					});
				})
				.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(RoundTripGrain).Assembly).WithReferences());
	}

	public class SiloBuilderConfigurator : ISiloBuilderConfigurator
	{
		public void Configure(ISiloHostBuilder hostBuilder)
			=> hostBuilder
				.AddMemoryGrainStorage("PubSubStore")
				.AddKafkaStreamProvider(Consts.KafkaStreamProvider, options =>
				{
					options.BrokerList = new List<string> { "pkc-l9pve.eu-west-1.aws.confluent.cloud:9092" };
					options.ConsumerGroupId = "TestGroup";
					options.Topics = new List<string> { Consts.StreamNamespace, Consts.StreamNamespace2 };
					options.PollTimeout = TimeSpan.FromMilliseconds(10);
					options.ExternalMessageIdentifier = "x-external-message";
					options.ConsumeMode = ConsumeMode.StreamEnd;
					options.WithSaslOptions(new Credentials
					{
						UserName = Environment.GetEnvironmentVariable("userName"),
						Password = Environment.GetEnvironmentVariable("password"),
						SslCaLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cacert.pem")
					});
				})
				.ConfigureApplicationParts(parts =>
					parts.AddApplicationPart(typeof(RoundTripGrain).Assembly).WithReferences());
	}
}
