using PublishResult.AppArgs;
using PublishResult.Processing;
using PublishResult.PublishToApi;
using PublishResult.PublishToPr;

namespace PublishResult;

public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("AHK Publish Result to GitHub PR\n");

        Console.WriteLine("Reading args...");
        var appArgs = ArgReader.GetArgs();
        Console.WriteLine("Reading args... done.\n");

        var dir = Directory.GetCurrentDirectory();
        Console.WriteLine($"Working directory is {dir}\n");

        Console.WriteLine("Processing...");
        var result = Processor.Process(appArgs, dir);
        Console.WriteLine("Processing... done.\n");

        Console.WriteLine("Publishing results to PR...");
        await PrPublisher.PublishToPrAsync(appArgs, result);
        Console.WriteLine("Publishing results to PR... done.\n");

        if (string.IsNullOrEmpty(appArgs.AhkAppUrl))
            Console.WriteLine("Sending result to Ahk Api not requested(AHK_APPURL not defined).\n");
        else if (!string.IsNullOrEmpty(appArgs.AhkAppToken) && !string.IsNullOrEmpty(appArgs.AhkAppSecret))
        {
            Console.WriteLine("Sending result to Ahk Api...");
            var apiResult = await ApiPublisher.PublishToApiAsync(appArgs, result);
            if (apiResult)
                Console.WriteLine("Sending result to Ahk Api... done.\n");
            else
                Console.WriteLine("Sending result to Ahk Api was not successful.\n");
        }
        else
            Console.WriteLine("Sending result to Ahk Api disabled.\n");

        Console.WriteLine("Finished. Bye.");
    }
}
