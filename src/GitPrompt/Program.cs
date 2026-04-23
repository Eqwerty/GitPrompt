using System.Text;
using GitPrompt.Commands;
using GitPrompt.Platform;
using GitPrompt.Prompting;

Console.OutputEncoding = Encoding.UTF8;

CommandRegistry.Dispatch(args);

var platformProvider = PlatformProvider.System;
var prompt = PromptBuilder.Build(platformProvider).Output;

Console.Write(prompt);

return 0;
