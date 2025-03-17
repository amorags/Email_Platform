using System;

namespace EmailCleanerAPI
{
    public interface IEmailCleanerService
    {
        int ProcessEmails(string inputDirectory);
    }
}