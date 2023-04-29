using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;

namespace FileMoverWithZipAndLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            // Source directory
            string sourceDirectory = @"C:\Users\epicd\Downloads";
            // Target directory
            string targetDirectory = @"C:\Users\epicd\Desktop\Data and Testing\Zipped Downloads";
            // Log file path
            string logFilePath = Path.Combine(@"C:\Users\epicd\Desktop\Data and Testing\Logs\FileMoverWithZipAndLogger", $"FileMoverWithZipAndLogger_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.log");

            // Zip all of the folders that exist within the source directory
            ZipDirectories(sourceDirectory, logFilePath);

            // Get all the files in the source directory and check if there are any files from the beginning
            if (Directory.GetFiles(sourceDirectory).Length == 0)
            {
                LogMessage("There are no files in the source directory.", logFilePath);
                return;
            }

            // Get all the files in the source directory
            var files = Directory.GetFiles(sourceDirectory);

            // Group the files by their extension
            var fileGroups = files.GroupBy(file => Path.GetExtension(file));

            // Create a dictionary to store the number of files with each name in each extension directory
            var fileCounts = new Dictionary<string, Dictionary<string, int>>();

            // Process each file group
            foreach (var fileGroup in fileGroups)
            {
                string extension = fileGroup.Key.TrimStart('.');
                string extensionDirectory = Path.Combine(targetDirectory, extension);
                string zipFilePath = Path.Combine(targetDirectory, $"{extension}.zip");

                // Check if a zip file for the extension already exists
                bool zipExists = File.Exists(zipFilePath);

                // If the zip file exists, unzip it to the extension directory
                if (zipExists)
                {
                    LogMessage($"Unzipping {zipFilePath}", logFilePath);
                    ZipFile.ExtractToDirectory(zipFilePath, extensionDirectory);

                    // Get the subdirectory created by default
                    string subdirectory = Directory.GetDirectories(extensionDirectory).FirstOrDefault();
                    if (subdirectory != null)
                    {
                        // Move the files out of the subdirectory and delete it
                        foreach (var file in Directory.GetFiles(subdirectory))
                        {
                            File.Move(file, Path.Combine(extensionDirectory, Path.GetFileName(file)));
                        }
                        Directory.Delete(subdirectory);
                    }

                    // Delete the zip file
                    LogMessage($"Deleting previous {zipFilePath}", logFilePath);
                    File.Delete(zipFilePath);
                }
                else
                {
                    LogMessage($"Creating directory: {extension}", logFilePath);
                    Directory.CreateDirectory(extensionDirectory);
                }

                // Get the count of each file name in the extension directory
                var existingFiles = Directory.GetFiles(extensionDirectory);
                var fileCount = new Dictionary<string, int>();

                foreach (var existingFile in existingFiles)
                {
                    string fileName = Path.GetFileName(existingFile);
                    if (fileCount.ContainsKey(fileName))
                    {
                        fileCount[fileName]++;
                    }
                    else
                    {
                        fileCount[fileName] = 1;
                    }
                }

                // Add the file count to the fileCounts dictionary for the extension
                fileCounts[extension] = fileCount;

                // Move the files into the extension directory
                foreach (var file in fileGroup)
                {
                    string fileName = Path.GetFileName(file);
                    string destinationFile = Path.Combine(extensionDirectory, fileName);

                    // If a file with the same name already exists, rename the new file
                    if (fileCounts[extension].ContainsKey(fileName))
                    {
                        int count = FindDuplicateFileName(extensionDirectory, fileName);
                        string newName = Path.GetFileNameWithoutExtension(fileName) + $" ({count})" + Path.GetExtension(fileName);
                        LogMessage($"Duplicate file found for {fileName}, renaming to {newName}", logFilePath);
                        destinationFile = Path.Combine(extensionDirectory, newName);
                        fileCounts[extension][fileName] = count;
                    }
                    else
                    {
                        fileCounts[extension][fileName] = 1;
                    }

                    // Log the file move
                    LogMessage($"Moving {fileName} to {destinationFile}", logFilePath);

                    // Move the file
                    File.Move(file, destinationFile);
                }

                // Zip the extension directory
                LogMessage($"Zipping {extensionDirectory} to {zipFilePath}", logFilePath);

                ZipFile.CreateFromDirectory(extensionDirectory, zipFilePath, CompressionLevel.Optimal, true);

                // Delete the extension directory
                Directory.Delete(extensionDirectory, true);
            }

            LogMessage("All files have been moved and zipped.", logFilePath);
            Console.ReadLine();
        }

        private static int FindDuplicateFileName(string directoryPath, string fileName)
        {
            // Extracts the name and extension of the file
            string nameOnly = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            // Initializes the count variable to 1
            int count = 1;

            // Loops through the directory and increments the count for each duplicate file name found
            while (File.Exists(Path.Combine(directoryPath, fileName)))
            {
                // Splits the file name into parts separated by '(' and ')' characters
                string[] parts = nameOnly.Split('(', ')');

                // If there are no parentheses in the name, add a count to the end of the name
                if (parts.Length == 1)
                {
                    nameOnly = $"{nameOnly} ({count})";
                }
                // If there are parentheses, search for a matching base name and add the count to that
                else
                {
                    bool foundMatch = false;
                    for (int i = parts.Length - 2; i >= 0; i -= 2)
                    {
                        string baseName = parts[i].Trim();

                        if (baseName == nameOnly)
                        {
                            nameOnly = string.Join("", parts.Take(i + 1)) + $" {count})";
                            foundMatch = true;
                            break;
                        }
                    }

                    // If there is no matching base name, add a count to the end of the name
                    if (!foundMatch)
                    {
                        nameOnly = $"{nameOnly} ({count})";
                    }
                }

                // Updates the file name with the new name and extension
                fileName = $"{nameOnly}{extension}";
                count++;
            }

            // Returns the highest count found for the file name
            return count - 1;
        }
        private static void LogMessage(string message, string logFilePath)
        {
            // Append the message to the log file
            using (var writer = File.AppendText(logFilePath))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }

            // Print the message to the console
            Console.WriteLine(message);
        }

        public static void ZipDirectories(string sourceDirectory, string logFilePath)
        {
            // Get all directories in the source directory
            string[] directories = Directory.GetDirectories(sourceDirectory);

            // Check if there are any directories in the source directory
            if (directories.Length == 0)
            {
                // If there are no directories, return
                LogMessage("No directories found in source directory.", logFilePath);
                return;
            }

            // Loop through each directory
            foreach (string directory in directories)
            {
                // Create a file name for the zip file by appending ".zip" to the directory name
                string zipFileName = directory + ".zip";

                // Check if a zip file with the same name already exists
                if (File.Exists(zipFileName))
                {
                    LogMessage($"{zipFileName} already exists, removing the original.", logFilePath);
                    // If it does, delete the original file
                    File.Delete(zipFileName);
                }

                // Create a new zip file from the directory
                ZipFile.CreateFromDirectory(directory, zipFileName, CompressionLevel.Optimal, true);

                LogMessage($"Creating a zip of {zipFileName}.", logFilePath);

                // Delete the original directory
                Directory.Delete(directory, true);
            }
        }
    }
}
