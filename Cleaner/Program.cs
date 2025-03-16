using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EmailCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string inputDirectory = @"C:\Users\alexs\Desktop\Cleaner\maildir";
            string outputDirectory = @"C:\Users\alexs\Desktop\Cleaner\cleaned_emails";

            int totalProcessed = 0;
            int totalSavedEmails = 0;

            Console.WriteLine($"Starting to process emails from: {inputDirectory}");
            Console.WriteLine($"Output will be saved to: {outputDirectory}");

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            foreach (string personFolder in Directory.GetDirectories(inputDirectory))
            {
                string personName = Path.GetFileName(personFolder);
                string sentMailFolder = Path.Combine(personFolder, "_sent_mail");

                if (!Directory.Exists(sentMailFolder))
                {
                    continue;
                }

                Console.WriteLine($"Processing emails for {personName}...");

                string personOutputFolder = Path.Combine(outputDirectory, personName);
                if (!Directory.Exists(personOutputFolder))
                {
                    Directory.CreateDirectory(personOutputFolder);
                }

                foreach (string emailPath in Directory.GetFiles(sentMailFolder))
                {
                    totalProcessed++;
                    string cleanedEmail = ExtractMetadataAndBody(emailPath);

                    if (!string.IsNullOrEmpty(cleanedEmail))
                    {
                        string originalFilename = Path.GetFileName(emailPath);
                        string cleanFilename = originalFilename + ".txt";
                        string outputPath = Path.Combine(personOutputFolder, cleanFilename);

                        File.WriteAllText(outputPath, cleanedEmail);
                        totalSavedEmails++;

                        if (totalSavedEmails % 100 == 0)
                        {
                            Console.WriteLine($"Processed {totalSavedEmails} emails so far...");
                        }
                    }
                }
            }

            Console.WriteLine($"\nEmail cleaning process completed!");
            Console.WriteLine($"Total emails processed: {totalProcessed}");
            Console.WriteLine($"Successfully cleaned and saved: {totalSavedEmails}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static string ExtractMetadataAndBody(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);

                // Preserve important metadata fields
                string[] metadataFields = { "Message-ID:", "Date:", "From:", "To:", "Subject:" };
                StringBuilder metadata = new StringBuilder();

                foreach (string field in metadataFields)
                {
                    Match match = Regex.Match(content, $"{field}.*", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        metadata.AppendLine(match.Value);
                    }
                }

                // Extract the body
                int bodyStart = content.IndexOf("\r\n\r\n");
                if (bodyStart < 0)
                {
                    bodyStart = content.IndexOf("\n\n");
                }

                string body = bodyStart >= 0 ? content.Substring(bodyStart).Trim() : string.Empty;

                // Remove forwarded email headers or excessive signature blocks
                body = Regex.Replace(body, @"(?s)([-=_]{5,}[\r\n].*?$)", "", RegexOptions.Singleline);

                return metadata.ToString() + "\n\n" + body;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}
