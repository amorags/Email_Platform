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

            // Update these paths to match your specific folder structure
            string inputDirectory = @"C:\Users\alexs\Desktop\Cleaner\maildir";
            string outputDirectory = @"C:\Users\alexs\Desktop\Cleaner\cleaned_emails";

            int totalProcessed = 0;
            int totalSavedEmails = 0;

            Console.WriteLine($"Starting to process emails from: {inputDirectory}");
            Console.WriteLine($"Output will be saved to: {outputDirectory}");
            
            // Make sure the output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Process each person's folder
            foreach (string personFolder in Directory.GetDirectories(inputDirectory))
            {
                string personName = Path.GetFileName(personFolder);
                string sentMailFolder = Path.Combine(personFolder, "_sent_mail");

                // Skip if the person doesn't have a sent_mail folder
                if (!Directory.Exists(sentMailFolder))
                {
                    continue;
                }

                Console.WriteLine($"Processing emails for {personName}...");

                // Create output folder for this person
                string personOutputFolder = Path.Combine(outputDirectory, personName);
                if (!Directory.Exists(personOutputFolder))
                {
                    Directory.CreateDirectory(personOutputFolder);
                }

                // Process each email in the sent_mail folder
                foreach (string emailPath in Directory.GetFiles(sentMailFolder))
                {
                    totalProcessed++;
                    string emailBody = ExtractEmailBody(emailPath);
                    
                    if (!string.IsNullOrEmpty(emailBody))
                    {
                        // Create a clean filename based on the original filename
                        string originalFilename = Path.GetFileName(emailPath);
                        string cleanFilename = originalFilename + ".txt";
                        
                        // Save the email body to a text file
                        string outputPath = Path.Combine(personOutputFolder, cleanFilename);
                        File.WriteAllText(outputPath, emailBody);
                        
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

        static string ExtractEmailBody(string filePath)
        {
            try
            {
                // Try different encodings if the default one fails
                string content = null;
                
                // List of encodings to try
                Encoding[] encodingsToTry = {
                    Encoding.UTF8,
                    Encoding.GetEncoding(1252), // Windows-1252
                    Encoding.ASCII,
                    Encoding.GetEncoding(28591) // ISO-8859-1
                };
                
                foreach (Encoding encoding in encodingsToTry)
                {
                    try
                    {
                        content = File.ReadAllText(filePath, encoding);
                        // If we get here without exception, we have successfully read the file
                        break;
                    }
                    catch
                    {
                        // Try the next encoding
                    }
                }
                
                if (content == null)
                {
                    // If all encodings failed, try reading as binary and convert to string
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        content = Encoding.ASCII.GetString(bytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to read file {filePath}: {ex.Message}");
                        return null;
                    }
                }
                
                // Extract body (everything after headers)
                // First find the end of headers with a double newline
                int bodyStart = -1;
                
                // Try different patterns for header/body separation
                string[] separators = { "\r\n\r\n", "\n\n" };
                
                foreach (string separator in separators)
                {
                    bodyStart = content.IndexOf(separator);
                    if (bodyStart >= 0)
                    {
                        bodyStart += separator.Length;
                        break;
                    }
                }
                
                if (bodyStart < 0)
                {
                    // If no clear separator found, try to find where headers end
                    int lastHeaderPos = -1;
                    string[] headerNames = { "Message-ID:", "Date:", "From:", "To:", "Subject:", "Mime-Version:", "Content-Type:", "X-From:", "X-To:", "X-cc:", "X-bcc:", "X-Folder:", "X-Origin:", "X-FileName:" };
                    
                    foreach (string header in headerNames)
                    {
                        int headerPos = content.LastIndexOf(header);
                        if (headerPos > lastHeaderPos)
                        {
                            lastHeaderPos = headerPos;
                        }
                    }
                    
                    if (lastHeaderPos >= 0)
                    {
                        // Find the end of this header line
                        int headerEnd = content.IndexOf('\n', lastHeaderPos);
                        if (headerEnd > lastHeaderPos)
                        {
                            // Body starts after this header
                            bodyStart = headerEnd + 1;
                        }
                    }
                }
                
                if (bodyStart < 0)
                {
                    // If still no body found, return empty
                    return string.Empty;
                }
                
                // Extract the body
                string body = content.Substring(bodyStart).Trim();
                
                // Remove any email signatures at the bottom containing common patterns
                body = Regex.Replace(body, @"[-=_]{5,}[\r\n].*?$", "", RegexOptions.Singleline);
                
                return body;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}