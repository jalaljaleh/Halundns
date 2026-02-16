namespace Halun.HalunDns.Windows
{
    public class PingResult
    {
        public string IPAddress { get; set; }
        public long RoundtripTime { get; set; }
        public bool IsSuccess { get; set; }
        public string Status { get; set; }
    }
}
