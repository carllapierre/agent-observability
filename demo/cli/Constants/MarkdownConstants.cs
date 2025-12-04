namespace AgentCLI.Constants;

public static class MarkdownConstants
{
    // Regex patterns for markdown syntax
    public static class Patterns
    {
        public const string Bold = @"\*\*(.+?)\*\*";
        public const string BoldUnderscore = @"__(.+?)__";
        public const string Italic = @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)";
        public const string ItalicUnderscore = @"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)";
        public const string InlineCode = @"`(.+?)`";
        public const string NumberedList = @"^\d+\.\s";
    }
    
    // Markdown syntax markers
    public const string CodeBlockDelimiter = "```";
    public const string Heading1 = "# ";
    public const string Heading2 = "## ";
    public const string Heading3 = "### ";
    public const string BulletDash = "- ";
    public const string BulletAsterisk = "* ";
    
    // Spectre markup replacements
    public const string BoldMarkup = "[bold]$1[/]";
    public const string ItalicMarkup = "[italic]$1[/]";
    public const string InlineCodeMarkup = "[grey on grey15]$1[/]";
    
    // List formatting
    public const string BulletChar = "•";
}

