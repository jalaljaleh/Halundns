// Halun DNS - DnsServerWindow
// Purpose: Window for adding/editing DNS server entries.
// Author: Jalal Jaleh
// License: MIT
using System;
using System.Windows;
using System.Windows.Input;

namespace Halun.HalunDns.Windows
{
    public partial class DnsServerWindow : Window
    {
        public Uri PathIcon = new Uri("pack://application:,,,/dns.ico", UriKind.RelativeOrAbsolute);

        public DnsServer ServerResult { get; private set; }

        public DnsServerWindow(DnsServer server = null)
        {
            InitializeComponent();
            DataContext = this;

            ServerResult = server ?? new DnsServer();

            if (server != null)
            {
                InputName.Text = server.Name;
                InputDnsAddress.Text = server.DnsAddress;
                InputDnsAddressAlt.Text = server.DnsAddressAlt;
                InputPriority.Text = server.Priority.ToString();
                InputType.Text = server.Type;
                InputDescription.Text = server.Description;

                Title = "Edit DNS Server";
            }
            else
            {
                Title = "Add New DNS Server";
            }

            btnConfirm.Click += BtnConfirm_Click;
            btnCancel.Click += BtnCancel_Click;
            btnClose.Click += (s, e) => this.Close();
            TitleBorder.MouseDown += TitleBorder_MouseDown;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(InputName.Text) || string.IsNullOrWhiteSpace(InputDnsAddress.Text))
            {
                MessageBox.Show("Please enter at least a Name and Primary DNS Address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            ServerResult.Name = InputName.Text.Trim();
            ServerResult.DnsAddress = InputDnsAddress.Text.Trim();
            ServerResult.DnsAddressAlt = InputDnsAddressAlt.Text?.Trim();
            ServerResult.Type = InputType.Text?.Trim() ?? "Custom";
            ServerResult.Description = InputDescription.Text?.Trim();


            if (int.TryParse(InputPriority.Text, out int priority))
            {
                ServerResult.Priority = priority;
            }
            else
            {
                ServerResult.Priority = 0;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TitleBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}