namespace PublishResult.Processing;

public class ImageFilesFinder
{
    public static string?[] GetImageFiles(string dir, string fileExtension)
    {
        try
        {
            var imageFiles = Directory.GetFiles(dir, $"*{fileExtension}", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Order()
                .ToArray();

            return imageFiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }
}
