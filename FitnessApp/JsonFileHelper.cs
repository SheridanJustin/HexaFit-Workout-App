using System.Reflection;
using System.IO;
using FitnessApp.Models;
using Newtonsoft.Json;

public class JsonFileHelper
{
    public static async Task CopyJsonFileToDeviceAsync(string fileName, string targetDirectory)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
       

        // Create the full resource name, adjust if necessary based on the logged names
        string resourceName = fileName; // Adjust if resource names don't have a prefix

        // Ensure the directory exists
        Directory.CreateDirectory(targetDirectory);

        // Define the full path for the file on the device
        string targetPath = Path.Combine(targetDirectory, fileName);

        // Read the embedded resource stream
        using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
        {
            if (resourceStream == null)
                throw new FileNotFoundException($"Resource {resourceName} not found in assembly.");

            // Copy the stream to the target path
            using (FileStream fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
            {
                await resourceStream.CopyToAsync(fileStream);
            }
        }
    }

    public static WorkoutSession LoadWorkoutSession(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<WorkoutSession>(json);
    }

}
