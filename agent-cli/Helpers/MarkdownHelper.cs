using Spectre.Console;
using System.Text.RegularExpressions;
using AgentCLI.Constants;

namespace AgentCLI.Helpers;

public static class MarkdownHelper
{
    public static void RenderMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return;

        var lines = markdown.Split('\n');
        bool inCodeBlock = false;
        bool lastWasList = false;
        bool lastWasBlank = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            bool isList = IsListItem(trimmed);
            bool isBlank = string.IsNullOrWhiteSpace(line);

            // Handle state transitions
            if (isList && !lastWasList && !lastWasBlank) AnsiConsole.WriteLine();
            if (!isList && lastWasList && !isBlank) AnsiConsole.WriteLine();

            // Render the line
            if (trimmed.StartsWith(MarkdownConstants.CodeBlockDelimiter))
            {
                inCodeBlock = !inCodeBlock;
                if (!inCodeBlock) AnsiConsole.WriteLine();
            }
            else if (inCodeBlock)
            {
                AnsiConsole.MarkupLine($"[{ColorConstants.CodeBlock}]{line.EscapeMarkup()}[/]");
            }
            else if (trimmed.StartsWith(MarkdownConstants.Heading3))
            {
                AnsiConsole.MarkupLine($"[bold underline {ColorConstants.Heading3}]{trimmed.Substring(4).EscapeMarkup()}[/]");
            }
            else if (trimmed.StartsWith(MarkdownConstants.Heading2))
            {
                AnsiConsole.MarkupLine($"[bold {ColorConstants.Heading2}]{trimmed.Substring(3).EscapeMarkup()}[/]");
            }
            else if (trimmed.StartsWith(MarkdownConstants.Heading1))
            {
                AnsiConsole.MarkupLine($"[bold {ColorConstants.Heading1}]{trimmed.Substring(2).EscapeMarkup()}[/]");
            }
            else if (isList)
            {
                RenderListItem(line, trimmed);
            }
            else if (!isBlank || !lastWasList)
            {
                AnsiConsole.MarkupLine(FormatInline(line));
            }

            lastWasList = isList;
            lastWasBlank = isBlank;
        }
    }

    private static bool IsListItem(string trimmed) =>
        trimmed.StartsWith(MarkdownConstants.BulletDash) ||
        trimmed.StartsWith(MarkdownConstants.BulletAsterisk) ||
        Regex.IsMatch(trimmed, MarkdownConstants.Patterns.NumberedList);

    private static void RenderListItem(string original, string trimmed)
    {
        const int baseIndent = 2; // Base indentation for all list items
        var additionalIndent = original.TakeWhile(char.IsWhiteSpace).Count();
        var totalIndent = baseIndent + additionalIndent;
        
        string content;
        if (trimmed.StartsWith(MarkdownConstants.BulletDash) || trimmed.StartsWith(MarkdownConstants.BulletAsterisk))
        {
            var text = FormatInline(trimmed.Substring(2));
            content = $"{MarkdownConstants.BulletChar} {text}";
        }
        else
        {
            content = FormatInline(trimmed);
        }
        
        // Use Padder to maintain indentation when text wraps
        var padding = new Padding(totalIndent, 0, 0, 0);
        var paddedContent = new Padder(new Markup(content), padding);
        AnsiConsole.Write(paddedContent);
        AnsiConsole.WriteLine();
    }

    private static string FormatInline(string text)
    {
        var result = text.EscapeMarkup();
        result = Regex.Replace(result, MarkdownConstants.Patterns.Bold, MarkdownConstants.BoldMarkup);
        result = Regex.Replace(result, MarkdownConstants.Patterns.BoldUnderscore, MarkdownConstants.BoldMarkup);
        result = Regex.Replace(result, MarkdownConstants.Patterns.Italic, MarkdownConstants.ItalicMarkup);
        result = Regex.Replace(result, MarkdownConstants.Patterns.ItalicUnderscore, MarkdownConstants.ItalicMarkup);
        result = Regex.Replace(result, MarkdownConstants.Patterns.InlineCode, MarkdownConstants.InlineCodeMarkup);
        return result;
    }
}

