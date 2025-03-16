using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace EmailCleaner
{
    class Program
    {
        public class CleanedEmail
        {
            [JsonPropertyName("message_id")]
            public string MessageId { get; set; } = "";

            [JsonPropertyName("date")]
            public string Date { get; set; } = "";

            [JsonPropertyName("from")]
            public string From { get; set; } = "";

            [JsonPropertyName("to")]
            public string To { get; set; } = "";

            [JsonPropertyName("cc")]
            public string Cc { get; set; } = "";

            [JsonPropertyName("bcc")]
            public string Bcc { get; set; } = "";

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = "";

            [JsonPropertyName("folder_path")]
            public string FolderPath { get; set; } = "";

            [JsonPropertyName("origin")]
            public string Origin { get; set; } = "";

            [JsonPropertyName("body")]
            public string Body { get; set; } = "";
        }

        static void Main(string[] args)
        {
            // Update these paths to match your specific folder structure
            string inputDirectory = @"C:\Users\alexs\Desktop\Cleaner\maildir";
            string outputDirectory = @"C:\Users\alexs\Desktop\Cleaner\cleaned_emails";

            int totalProcessed = 0;
            int successful = 0;
            int failed = 0;

            Console.WriteLine($"Starting to process emails from: {inputDirectory}");
            Console.WriteLine($"Output will be saved to: {outputDirectory}");
            
            ProcessDirectory(inputDirectory, outputDirectory, ref totalProcessed, ref successful, ref failed);

            Console.WriteLine($"\nEmail cleaning process completed!");
            Console.WriteLine($"Total emails processed: {totalProcessed}");
            Console.WriteLine($"Successfully cleaned: {successful}");
            Console.WriteLine($"Failed to process: {failed}");
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void ProcessDirectory(string inputDir, string outputDir, ref int totalProcessed, ref int successful, ref int failed)
        {
            try
            {
                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Process all files in the current directory
                string[] files = Directory.GetFiles(inputDir);
                foreach (string filePath in files)
                {
                    // Check if the file exists and has content
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Exists || fileInfo.Length == 0)
                    {
                        Console.WriteLine($"Skipping empty or non-existent file: {filePath}");
                        continue;
                    }
                    
                    string fileName = Path.GetFileName(filePath);
                    
                    // Skip certain files that are likely not emails
                    if (fileName.StartsWith(".") || 
                        fileName.EndsWith(".txt") || 
                        fileName.EndsWith(".doc") || 
                        fileName.EndsWith(".pdf") ||
                        fileName.EndsWith(".zip") ||
                        fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    totalProcessed++;
                    
                    try
                    {
                        CleanedEmail cleanedEmail = CleanEmail(filePath);
                        if (cleanedEmail != null)
                        {
                            successful++;
                            
                            // Create filename based on date, sender, and subject
                            string dateFormatted = ExtractDate(cleanedEmail.Date);
                            
                            // Clean sender
                            string senderClean = "unknown_sender";
                            Match senderMatch = Regex.Match(cleanedEmail.From, @"([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})");
                            if (senderMatch.Success)
                            {
                                senderClean = senderMatch.Groups[1].Value.Split('@')[0];
                            }
                            
                            // Clean subject
                            string subject = string.IsNullOrEmpty(cleanedEmail.Subject) ? "no_subject" : cleanedEmail.Subject;
                            subject = Regex.Replace(subject, @"[^\w\s-]", "").Trim().Replace(" ", "_");
                            if (subject.Length > 30)
                                subject = subject.Substring(0, 30);
                            
                            if (string.IsNullOrEmpty(subject))
                                subject = "no_subject";
                            
                            // Include original person folder in the output path
                            string personFolder = GetPersonFolder(filePath, inputDir);
                            string subFolder = GetSubFolder(filePath, inputDir);
                            
                            string relativeOutputDir = Path.Combine(personFolder, subFolder);
                            string fullOutputDir = Path.Combine(outputDir, relativeOutputDir);
                            
                            if (!Directory.Exists(fullOutputDir))
                            {
                                Directory.CreateDirectory(fullOutputDir);
                            }
                            
                            string outputFilename = $"{dateFormatted}_{senderClean}_{subject}.json";
                            string outputFilePath = Path.Combine(fullOutputDir, outputFilename);
                            
                            // Write cleaned data to output file
                            string jsonString = JsonSerializer.Serialize(cleanedEmail, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(outputFilePath, jsonString);
                            
                            if (successful % 100 == 0)
                            {
                                Console.WriteLine($"Processed {successful} emails successfully...");
                            }
                        }
                        else
                        {
                            failed++;
                            if (failed % 1000 == 0)
                            {
                                Console.WriteLine($"Failed to process {failed} emails so far...");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                        failed++;
                    }
                }

                // Process all subdirectories
                string[] subDirs = Directory.GetDirectories(inputDir);
                foreach (string subDir in subDirs)
                {
                    ProcessDirectory(subDir, outputDir, ref totalProcessed, ref successful, ref failed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory {inputDir}: {ex.Message}");
            }
        }

        static string GetPersonFolder(string filePath, string rootDir)
        {
            try
            {
                string relativePath = filePath.Substring(rootDir.Length).TrimStart('\\');
                string[] parts = relativePath.Split('\\');
                
                if (parts.Length > 0)
                {
                    return parts[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting person folder from {filePath}: {ex.Message}");
            }
            
            return "unknown";
        }
        
        static string GetSubFolder(string filePath, string rootDir)
        {
            try
            {
                string relativePath = filePath.Substring(rootDir.Length).TrimStart('\\');
                string[] parts = relativePath.Split('\\');
                
                // Skip the first part (person) and the last part (filename)
                if (parts.Length > 2)
                {
                    return string.Join("\\", parts.Skip(1).Take(parts.Length - 2));
                }
                else if (parts.Length == 2)
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting subfolder from {filePath}: {ex.Message}");
            }
            
            return "";
        }

        static CleanedEmail CleanEmail(string filePath)
        {
            try
            {
                // Try different encodings if the default one fails
                string content = null;
                Exception lastException = null;
                
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
                    catch (Exception ex)
                    {
                        lastException = ex;
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
                        throw new Exception($"Failed to read file with any encoding: {lastException?.Message}", ex);
                    }
                }
                
                // Check if content looks like an email (has some basic headers)
                if (!content.Contains("From:") && !content.Contains("Date:") && !content.Contains("To:"))
                {
                    return null; // Not an email
                }
                
                CleanedEmail email = new CleanedEmail();
                
                // Extract headers using regex
                email.MessageId = ExtractHeaderValue(content, "Message-ID");
                email.Date = ExtractHeaderValue(content, "Date");
                email.From = ExtractHeaderValue(content, "From");
                email.To = ExtractHeaderValue(content, "To");
                email.Cc = ExtractHeaderValue(content, "Cc");
                email.Bcc = ExtractHeaderValue(content, "Bcc");
                email.Subject = ExtractHeaderValue(content, "Subject");
                email.FolderPath = ExtractHeaderValue(content, "X-Folder");
                email.Origin = ExtractHeaderValue(content, "X-Origin");
                
                // Extract body (everything after headers)
                // First try double line break
                string[] parts = content.Split(new[] { "\n\n" }, 2, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    email.Body = parts[1].Trim();
                }
                else
                {
                    // If no double line break, try to find where headers end
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
                            email.Body = content.Substring(headerEnd + 1).Trim();
                        }
                    }
                }
                
                return email;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                return null;
            }
        }

        static string ExtractHeaderValue(string content, string headerName)
        {
            try
            {
                string pattern = $"{headerName}:\\s*(.*?)(?:\\r?\\n(?!\\s)|\\r?\\n\\r?\\n)";
                Match match = Regex.Match(content, pattern, RegexOptions.Singleline);
                
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
                
                // Try an alternative pattern for multi-line headers
                pattern = $"{headerName}:\\s*(.*?)(?:\\r?\\n(?![\\t ])|\\r?\\n\\r?\\n)";
                match = Regex.Match(content, pattern, RegexOptions.Singleline);
                
                if (match.Success)
                {
                    // Clean up multi-line headers (remove line breaks and excessive whitespace)
                    string value = match.Groups[1].Value;
                    value = Regex.Replace(value, @"\r?\n[\t ]+", " ");
                    return value.Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting header {headerName}: {ex.Message}");
            }
            
            return string.Empty;
        }

        static string ExtractDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return "unknown_date";
                
            try
            {
                // Try parsing standard date formats
                if (DateTime.TryParse(dateStr, out DateTime date))
                {
                    return date.ToString("yyyyMMdd_HHmmss");
                }
                
                // Fall back to regex patterns
                string[] patterns = new[]
                {
                    @"(\d{1,2})\s+(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{4})",
                    @"(\d{4})-(\d{1,2})-(\d{1,2})",
                    @"(\d{1,2})/(\d{1,2})/(\d{4})"
                };
                
                foreach (string pattern in patterns)
                {
                    if (Regex.IsMatch(dateStr, pattern))
                    {
                        return "date_found_in_string";
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            
            return "unknown_date";
        }
    }
}