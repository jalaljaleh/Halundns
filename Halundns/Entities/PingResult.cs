// Halun DNS - PingResult
// Purpose: Represents the result of a ping operation performed by the
// DnsPingHelper. Contains success flag and roundtrip time.
// Author: Jalal Jaleh
// License: MIT - see LICENSE.txt in repository root
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
