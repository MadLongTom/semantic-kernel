using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;

namespace AutoInvokeBufferTest
{
    public class KernelFactory(IConfiguration configuration, ILogger<KernelFactory> logger)
    {
        public async Task<Kernel> CreateAgentKernelAsync()
        {
            bool flag = this._cachedAgentKernel != null;
            Kernel kernel;
            if (flag)
            {
                kernel = this._cachedAgentKernel;
            }
            else
            {
                string endpoint = configuration.GetSection("LLMProvider")["AgentEndpoint"];
                string model = configuration.GetSection("LLMProvider")["AgentModel"];
                string embeddingEndpoint = configuration.GetSection("LLMProvider")["AgentEndpoint"];
                IKernelBuilder builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010 
                builder.AddOpenAIChatCompletion(model, new Uri(endpoint), configuration.GetSection("LLMProvider")["AgentApiKey"], null, null, null);
#pragma warning restore SKEXP0010 
                this._cachedAgentKernel = builder.Build();
                KernelFunction timefunc = this._cachedAgentKernel.CreateFunctionFromMethod(new Func<string, string>((string callid) => DateTime.Now.ToString()), "GetCurrentTime", "Get the current date and time.", null, null);
                this._cachedAgentKernel.ImportPluginFromFunctions("TimeUtil", [timefunc]);
                kernel = this._cachedAgentKernel;
            }
            return kernel;
        }

        // Token: 0x06000005 RID: 5 RVA: 0x00002100 File Offset: 0x00000300
        public async Task ChatCompletionAsStreamAsync(Kernel kernel, ChatHistory chatHistory, OpenAIPromptExecutionSettings? requestSettings = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            requestSettings ??= new OpenAIPromptExecutionSettings();
            requestSettings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
            IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>(null);
            string fullMessage = string.Empty;
#pragma warning disable SKEXP0010 
            IAsyncEnumerable<StreamingChatMessageContent> results = chatHistory.AddStreamingMessageAsync(chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, requestSettings, kernel, cancellationToken).Cast<OpenAIStreamingChatMessageContent>(), true);
#pragma warning restore SKEXP0010 
            IAsyncEnumerator<StreamingChatMessageContent> asyncEnumerator = results.GetAsyncEnumerator(default(CancellationToken));
            object obj = null;

            while (await asyncEnumerator.MoveNextAsync())
            {
                StreamingChatMessageContent completionResult = asyncEnumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                fullMessage += completionResult.Content;
                logger.LogDebug(JsonSerializer.Serialize(completionResult), Array.Empty<object>());
                completionResult = null;
            }
            obj = null;
            asyncEnumerator = null;
            this.LogChatHistory(chatHistory);
        }

        // Token: 0x06000006 RID: 6 RVA: 0x00002164 File Offset: 0x00000364
        private void LogChatHistory(ChatHistory chatHistory)
        {
            logger.LogWarning("Print Chat History：", Array.Empty<object>());
            foreach (ChatMessageContent message in ((IEnumerable<ChatMessageContent>)chatHistory))
            {
                bool flag = message.Role == AuthorRole.Assistant;
                if (flag)
                {
                    FunctionCallContent content = (FunctionCallContent)message.Items.FirstOrDefault((KernelContent i) => i is FunctionCallContent);
                    bool flag2 = content != null;
                    if (flag2)
                    {
                        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new(40, 3);
                        defaultInterpolatedStringHandler.AppendLiteral("\r\nPluginName：");
                        defaultInterpolatedStringHandler.AppendFormatted(content.PluginName);
                        defaultInterpolatedStringHandler.AppendLiteral("\r\nFunctionName：");
                        defaultInterpolatedStringHandler.AppendFormatted(content.FunctionName);
                        defaultInterpolatedStringHandler.AppendLiteral("\r\nArguments：");
                        defaultInterpolatedStringHandler.AppendFormatted(string.Join(";", content.Arguments.Select(delegate (KeyValuePair<string, object> arg)
                        {
                            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new(1, 2);
                            defaultInterpolatedStringHandler2.AppendFormatted(arg.Key);
                            defaultInterpolatedStringHandler2.AppendLiteral(":");
                            defaultInterpolatedStringHandler2.AppendFormatted<object>(arg.Value);
                            return defaultInterpolatedStringHandler2.ToStringAndClear();
                        })));
                        string output = defaultInterpolatedStringHandler.ToStringAndClear();
                        logger.LogInformation("FunctionCall:\r\n Role = {Role}\r\n Content = {Content}", new object[] { message.Role, output });
                    }
                    logger.LogInformation("Completion:\r\n Role = {Role}\r\n Content = {Content}", new object[] { message.Role, message.Content });
                }
                else
                {
                    bool flag3 = message.Role == AuthorRole.Tool;
                    if (flag3)
                    {
                        FunctionResultContent content2 = (FunctionResultContent)message.Items.First((KernelContent i) => i is FunctionResultContent);
                        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new(40, 3);
                        defaultInterpolatedStringHandler.AppendLiteral("\r\nPluginName：");
                        defaultInterpolatedStringHandler.AppendFormatted(content2.PluginName);
                        defaultInterpolatedStringHandler.AppendLiteral("\r\nFunctionName：");
                        defaultInterpolatedStringHandler.AppendFormatted(content2.FunctionName);
                        defaultInterpolatedStringHandler.AppendLiteral("\r\nArguments：");
                        defaultInterpolatedStringHandler.AppendFormatted<object>(content2.Result);
                        string output2 = defaultInterpolatedStringHandler.ToStringAndClear();
                        logger.LogInformation("FunctionResult:\r\n Role = {Role}\r\n Content = {Content}", new object[] { message.Role, output2 });
                    }
                    else
                    {
                        bool flag4 = message.Role == AuthorRole.System;
                        if (flag4)
                        {
                            logger.LogInformation("System:\r\n Role = {Role}\r\n Content = {Content}", new object[] { message.Role, message.Content });
                        }
                        else
                        {
                            bool flag5 = message.Role == AuthorRole.User;
                            if (flag5)
                            {
                                logger.LogInformation("User:\r\n Role = {Role}\r\n Content = {Content}", new object[] { message.Role, message.Content });
                            }
                            else
                            {
                                bool flag6 = message.Role == AuthorRole.Developer;
                                if (flag6)
                                {
                                    logger.LogInformation("Dev:\r\n Role = {Role}\r\n Content = {Content}", new object[] { message.Role, message.Content });
                                }
                            }
                        }
                    }
                }
            }
            logger.LogWarning("Chat Complete", Array.Empty<object>());
        }

        private Kernel? _cachedAgentKernel;
    }
}
