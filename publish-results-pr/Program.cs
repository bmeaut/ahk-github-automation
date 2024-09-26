using PublishResult.AppArgs;
using PublishResult.Processing;
using PublishResult.PublishToPr;

namespace PublishResult;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("AHK Publish Result to GitHub PR\n");

        Console.WriteLine("Reading args...");
        var appArgs = ArgReader.GetArgs();
        //Console.WriteLine(appArgs);
        Console.WriteLine("Reading args... done.\n");

        var dir = Directory.GetCurrentDirectory();
        Console.WriteLine($"Working directory is {dir}\n");

        Console.WriteLine("Processing...");
        var result = Processor.Process(appArgs, dir);
        Console.WriteLine("Processing... done.\n");

        Console.WriteLine("Publishing results to PR...");
        await PrPublisher.PublishToPrAsync(appArgs, result);
        Console.WriteLine("Publishing results to PR... done.\n");

        // TODO: Uncomment if backend is ready

        //if (appArgs.AhkAppUrl != "" && appArgs.AhkAppToken != "" && appArgs.AhkAppSecret != "")
        //{
        //    Console.WriteLine("Sending result to Ahk Api...");
        //    var apiPublisher = new ApiPublisher();
        //    apiPublisher.PublishToApi(appArgs, result);
        //    Console.WriteLine("Sending result to Ahk Api... done.\n");
        //}
        //else
        //    Console.WriteLine("Sending result to Ahk Api disabled.\n");

        Console.WriteLine("Finished. Bye.");
    }
}
