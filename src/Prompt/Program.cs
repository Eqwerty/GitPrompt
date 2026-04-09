using System.Text;
using Prompt.Git;
using Prompt.Platform;
using Prompt.Prompting;
using static Prompt.Constants.PromptColors;

Console.OutputEncoding = Encoding.UTF8;

var platformProvider = PlatformProvider.System;

var contextSegment = ContextSegmentBuilder.Build(platformProvider);
var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync();
var promptSymbol = PromptSymbolBuilder.Build(platformProvider);

var promptLine = string.IsNullOrEmpty(gitStatusSegment)
    ? contextSegment
    : $"{contextSegment} {gitStatusSegment}";

Console.Write($"{promptLine}\n{ColorPromptSymbol}{promptSymbol}{ColorReset} ");

return 0;
