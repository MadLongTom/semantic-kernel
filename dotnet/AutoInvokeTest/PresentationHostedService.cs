using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AutoInvokeBufferTest
{
    public class PresentationHostedService(KernelFactory kernelFactory, ILogger<PresentationHostedService> logger) : IHostedService
    {
        public async Task ChatWithAgentAsync()
        {
            Kernel kernel = await kernelFactory.CreateAgentKernelAsync();
            Kernel agent = kernel;
            ChatHistory chatMessageContents = new ChatHistory("you are a helpful assistant.");
            chatMessageContents.AddUserMessage("what time is it now?");
            OpenAIPromptExecutionSettings chatRequestSettings = new()
            {
                ExtensionData = new Dictionary<string, object>(),
                MaxTokens = new int?(4096),
                Temperature = new double?(0.6),
                TopP = new double?(0.95)
            };
            await kernelFactory.ChatCompletionAsStreamAsync(agent, chatMessageContents, chatRequestSettings, default(CancellationToken));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.ChatWithAgentAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
