using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Client
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread listenThread;
        private string userName;
        private bool isTyping = false;
        private DispatcherTimer typingTimer;

        public MainWindow()
        {
            InitializeComponent();
            btnSend.IsEnabled = false; // Disable send button until connected

            // Timer to reset typing status
            typingTimer = new DispatcherTimer();
            typingTimer.Interval = TimeSpan.FromSeconds(1.5);
            typingTimer.Tick += TypingTimer_Tick;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                MessageBox.Show("Enter your name before connecting.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                client = new TcpClient("127.0.0.1", 12345);
                stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                userName = txtUserName.Text;
                writer.WriteLine(userName); // Send name to server

                btnConnect.IsEnabled = false;
                btnSend.IsEnabled = true;

                listenThread = new Thread(ListenForMessages);
                listenThread.IsBackground = true;
                listenThread.Start();

                MessageBox.Show("Connected to chat server!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not connect: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text))
                return;

            writer.WriteLine(txtMessage.Text);
            txtMessage.Clear();
        }

        private void txtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isTyping)
            {
                isTyping = true;
                writer.WriteLine($"TYPING:{userName}");
            }

            typingTimer.Stop();
            typingTimer.Start();
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            isTyping = false;
            writer.WriteLine($"STOP_TYPING:{userName}");
            typingTimer.Stop();
        }

        private void ListenForMessages()
        {
            try
            {
                while (true)
                {
                    string message = reader.ReadLine();
                    if (message == null) break;

                    Dispatcher.Invoke(() =>
                    {
                        if (message.StartsWith("TYPING:"))
                        {
                            string typingUser = message.Substring(7);
                            txtTypingIndicator.Text = $"{typingUser} is typing...";
                        }
                        else if (message.StartsWith("STOP_TYPING:"))
                        {
                            txtTypingIndicator.Text = "";
                        }
                        else if (message.StartsWith("USER_LIST:"))
                        {
                            UpdateUserList(message.Substring(10)); // Update the user list
                        }
                        else
                        {
                            txtChat.AppendText(message + Environment.NewLine);
                            txtChat.ScrollToEnd();
                        }
                    });
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Disconnected from server.", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    btnConnect.IsEnabled = true;
                    btnSend.IsEnabled = false;
                });
            }
        }

        private void UpdateUserList(string users)
        {
            // Clear the list and add the updated users
            lstUsers.Items.Clear();
            string[] userList = users.Split(',');
            foreach (var user in userList)
            {
                lstUsers.Items.Add(user);
            }
        }
    }
}