﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace EmailCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string inputDirectory = @"C:\\Users\\alexs\\Desktop\\Cleaner\\maildir";
            int totalProcessed = 0;
            
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "cleaned_emails", durable: true, exclusive: false, autoDelete: false, arguments: null);

                foreach (string personFolder in Directory.GetDirectories(inputDirectory))
                {
                    string personName = Path.GetFileName(personFolder);
                    string sentMailFolder = Path.Combine(personFolder, "_sent_mail");

                    if (!Directory.Exists(sentMailFolder))
                        continue;

                    Console.WriteLine($"Processing emails for {personName}...");
                    
                    foreach (string emailPath in Directory.GetFiles(sentMailFolder))
                    {
                        totalProcessed++;
                        EmailMessage email = ExtractMetadataAndBody(emailPath);

                        if (email != null)
                        {
                            string messageBody = JsonConvert.SerializeObject(email);
                            var body = Encoding.UTF8.GetBytes(messageBody);

                            channel.BasicPublish(exchange: "", routingKey: "cleaned_emails", basicProperties: null, body: body);
                        }

                        if (totalProcessed % 100 == 0)
                            Console.WriteLine($"Processed {totalProcessed} emails so far...");
                    }
                }
            }

            Console.WriteLine($"\nEmail cleaning and publishing completed! Processed: {totalProcessed} emails.");
        }

        static EmailMessage ExtractMetadataAndBody(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);

                string sender = ExtractField(content, "From:");
                string recipient = ExtractField(content, "To:");
                string dateStr = ExtractField(content, "Date:");
                DateTime date = DateTime.TryParse(dateStr, out var parsedDate) ? parsedDate : DateTime.MinValue;

                int bodyStart = content.IndexOf("\r\n\r\n");
                if (bodyStart < 0) bodyStart = content.IndexOf("\n\n");
                string body = bodyStart >= 0 ? content.Substring(bodyStart).Trim() : string.Empty;
                body = Regex.Replace(body, "(?s)([-=_]{5,}["]")", "", RegexOptions.Singleline);

                return new EmailMessage { Sender = sender, Recipient = recipient, Date = date, Body = body };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                return null;
            }
        }

        static string ExtractField(string content, string field)
        {
            Match match = Regex.Match(content, $"{field}.*", RegexOptions.IgnoreCase);
            return match.Success ? match.Value.Replace(field, "").Trim() : "Unknown";
        }
    }
}