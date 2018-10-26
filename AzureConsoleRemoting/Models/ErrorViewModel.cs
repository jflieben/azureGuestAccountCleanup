using System;

namespace AzureConsoleRemoting.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string description { get; set; }

        public Exception debugData { get; set; }
    }
}