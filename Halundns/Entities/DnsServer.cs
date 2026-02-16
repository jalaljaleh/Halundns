using System.ComponentModel;

namespace Halun.HalunDns.Windows
{
    public class DnsServer : INotifyPropertyChanged
    {
        private string _ping = "";
        public string Ping
        {
            get => _ping;
            set { _ping = value; OnPropertyChanged(nameof(Ping)); }
        }
        private string _bypass = "";
        public string Bypass
        {
            get => _bypass;
            set { _bypass = value; OnPropertyChanged(nameof(Bypass)); }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string DnsAddress { get; set; }
        public string DnsAddressAlt { get; set; }
        public string Type { get; set; }
        public int Priority { get; set; }

        public override string ToString()
        {
            return
                "\n" +
                $"Name: {Name}\n" +
                $"Type: {Type}\n" +
                $"Primary   Address: {DnsAddress}\n" +
                $"Secondary Address: {DnsAddressAlt}\n" +
                "";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}