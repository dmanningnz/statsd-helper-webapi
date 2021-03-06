﻿using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using FakeItEasy;
using NUnit.Framework;
using StatsDHelper.WebApi.Services;

namespace StatsDHelper.WebApi.Tests.Integration
{
    abstract class BaseInstrumentationServiceTests
    {
        protected HttpActionExecutedContext HttpActionExecutedContext;
        protected static CancellationTokenSource CancellationTokenSource;
        protected CancellationToken CancellationToken;

        private UdpClient _udpClient;
        protected IInstrumentationService InstrumentationService;
        protected IAppSettings AppSettings;

        [SetUp]
        public void SetUp()
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;
            HttpActionExecutedContext = SetUpFakeHttpActionContext();

            AppSettings = A.Fake<IAppSettings>();

            InstrumentationService = new InstrumentationService(AppSettings);
        }

        protected HttpActionExecutedContext SetUpFakeHttpActionContext()
        {
            var port = int.Parse(ConfigurationManager.AppSettings["StatsD.Port"]);

            var actionContext = new HttpActionContext { ActionDescriptor = new FakeActionDescriptor(), ControllerContext = new HttpControllerContext { Request = new HttpRequestMessage() } };

            var httpActionExecutedContext = new HttpActionExecutedContext
            {
                ActionContext = actionContext,
                Response = new HttpResponseMessage()
            };

            var requestStopwatch = new Stopwatch();
            actionContext.Request.Properties.Add(Constants.StopwatchKey, requestStopwatch);
            requestStopwatch.Start();

            _udpClient = new UdpClient(port);

            return httpActionExecutedContext;
        }

        protected async Task<List<string>> ListenForTwoStatsDMessages()
        {
            return await Task.Run(async () =>
            {
                var result = new List<UdpReceiveResult>();

                while (result.Count != 2)
                {
                    result.Add(await _udpClient.ReceiveAsync());
                }

                return result.Select(o => Encoding.UTF8.GetString(o.Buffer)).ToList();


            }, CancellationToken);
        }

        [TearDown]
        public void CancelUdpListener()
        {
            _udpClient.Close();
            CancellationTokenSource.Cancel();
        }        
    }
}