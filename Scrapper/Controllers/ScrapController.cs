using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EmailCleanerAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailCleanerController : ControllerBase
    {
        private readonly IEmailCleanerService _emailCleanerService;

        public EmailCleanerController(IEmailCleanerService emailCleanerService)
        {
            _emailCleanerService = emailCleanerService;
        }

        [HttpPost("clean")] 
        public IActionResult CleanEmails([FromBody] string inputDirectory)
        {
            if (string.IsNullOrWhiteSpace(inputDirectory) || !Directory.Exists(inputDirectory))
            {
                return BadRequest("Invalid directory path.");
            }

            int processedCount = _emailCleanerService.ProcessEmails(inputDirectory);
            return Ok($"Processing completed. Total emails processed: {processedCount}");
        }
    }

}