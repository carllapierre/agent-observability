namespace AgentCLI.Helpers;

public static class SpinnerHelper
{
    private static readonly string[] SpinnerChars = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
    private const int AnimationDelayMs = 80;

    /// <summary>
    /// Runs an async task with an inline spinner animation.
    /// The spinner appears at the current cursor position.
    /// </summary>
    public static async Task<T> RunWithSpinnerAsync<T>(Func<Task<T>> taskFunc, string message)
    {
        var cts = new CancellationTokenSource();
        int spinnerIndex = 0;
        
        var spinnerTask = Task.Run(async () =>
        {
            var cursorLeft = Console.CursorLeft;
            var cursorTop = Console.CursorTop;
            
            while (!cts.Token.IsCancellationRequested)
            {
                Console.SetCursorPosition(cursorLeft, cursorTop);
                Console.Write($"{SpinnerChars[spinnerIndex]} {message}");
                spinnerIndex = (spinnerIndex + 1) % SpinnerChars.Length;
                
                try
                {
                    await Task.Delay(AnimationDelayMs, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            
            // Clear the spinner and message
            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.Write(new string(' ', message.Length + 2));
            Console.SetCursorPosition(cursorLeft, cursorTop);
        });
        
        var result = await taskFunc();
        cts.Cancel();
        await spinnerTask;
        
        return result;
    }
}

