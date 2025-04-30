using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AlrawiChat
{
    public partial class MainForm : Form
    {
        // Constants
        private const int HEADER = 64;
        private const string FORMAT = "utf-8";
        private const string DISCONNECT_MESSAGE = "!DISCONNECT";

        // Variables
        private bool isServer = false;
        private Socket server = null;
        private TcpClient client = null;
        private NetworkStream clientStream = null;
        private Thread listenThread = null;
        private Thread messageThread = null;
        private string username;

        // Server specific variables
        private static List<Socket> clients = new List<Socket>();
        private static List<IPEndPoint> addrs = new List<IPEndPoint>();

        public MainForm()
        {
            InitializeComponent();
            SetupComponents();
            username = Environment.MachineName;
        }

        private void SetupComponents() // Renamed from InitializeComponent
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Alrawi Chat";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 450);
            using (MemoryStream ms = new MemoryStream(Properties.Resources.alrawiChatIcon))
            {
                this.Icon = new Icon(ms);
            }

            this.FormClosing += MainForm_FormClosing;

            // Initialize welcome panel
            InitializeWelcomePanel();

            this.ResumeLayout(false);
        }

        #region UI Initialization Methods

        private void InitializeWelcomePanel()
        {
            Panel welcomePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Label titleLabel = new Label
            {
                Text = "Alrawi Chat",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(0, 20, 0, 0)
            };

            Label subtitleLabel = new Label
            {
                Text = "Local WiFi Messaging Application",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30
            };
            PictureBox logoBox = new PictureBox
            {

                //Image = GetChatLogo(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(120, 120),
                Location = new Point((800 - 120) / 2, 140)
            };

            byte[] imageBytes = Properties.Resources.alrawiChatImage;
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                logoBox.Image = Image.FromStream(ms);
            }
            Button serverButton = new Button
            {
                Text = "Start as Server",
                Size = new Size(200, 50),
                Location = new Point((800 - 420) / 2, 300),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                FlatStyle = FlatStyle.Flat
            };
            serverButton.FlatAppearance.BorderSize = 0;
            serverButton.Click += (s, e) => { ShowServerSetupPanel(); };

            Button clientButton = new Button
            {
                Text = "Connect as Client",
                Size = new Size(200, 50),
                Location = new Point((800 - 420) / 2 + 220, 300),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                FlatStyle = FlatStyle.Flat
            };
            clientButton.FlatAppearance.BorderSize = 0;
            clientButton.Click += (s, e) => { ShowClientSetupPanel(); };

            Label usernameLabel = new Label
            {
                Text = "Your Username:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point((800 - 400) / 2, 380),
                Size = new Size(150, 30),
                TextAlign = ContentAlignment.MiddleRight
            };

            TextBox usernameTextBox = new TextBox
            {
                Text = username,
                Font = new Font("Segoe UI", 10),
                Location = new Point((800 - 400) / 2 + 160, 385),
                Size = new Size(290, 30)
            };
            usernameTextBox.TextChanged += (s, e) => { username = usernameTextBox.Text; };

            welcomePanel.Controls.Add(clientButton);
            welcomePanel.Controls.Add(serverButton);
            welcomePanel.Controls.Add(logoBox);
            welcomePanel.Controls.Add(subtitleLabel);
            welcomePanel.Controls.Add(titleLabel);
            welcomePanel.Controls.Add(usernameLabel);
            welcomePanel.Controls.Add(usernameTextBox);

            this.Controls.Add(welcomePanel);
            this.Tag = welcomePanel;
        }

        private void ShowServerSetupPanel()
        {
            Panel serverPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Label titleLabel = new Label
            {
                Text = "Start Server",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            Label ipLabel = new Label
            {
                Text = "Your IP Address:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point((800 - 400) / 2, 100),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.MiddleRight
            };

            string localIP = GetLocalIPAddress();
            TextBox ipTextBox = new TextBox
            {
                Text = localIP,
                Font = new Font("Segoe UI", 10),
                Location = new Point((800 - 400) / 2 + 130, 100),
                Size = new Size(270, 30),
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Label portLabel = new Label
            {
                Text = "Port:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point((800 - 400) / 2, 150),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.MiddleRight
            };

            TextBox portTextBox = new TextBox
            {
                Text = "5050",
                Font = new Font("Segoe UI", 10),
                Location = new Point((800 - 400) / 2 + 130, 150),
                Size = new Size(270, 30)
            };

            Button startServerButton = new Button
            {
                Text = "Start Server",
                Size = new Size(200, 50),
                Location = new Point((800 - 200) / 2, 220),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                FlatStyle = FlatStyle.Flat
            };
            startServerButton.FlatAppearance.BorderSize = 0;
            startServerButton.Click += (s, e) =>
            {
                int port;
                if (!int.TryParse(portTextBox.Text, out port))
                {
                    MessageBox.Show("Please enter a valid port number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                isServer = true;
                StartServer(localIP, port);
                ShowChatPanel();
            };

            Button backButton = new Button
            {
                Text = "Back",
                Size = new Size(100, 40),
                Location = new Point((800 - 100) / 2, 300),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat
            };
            backButton.FlatAppearance.BorderSize = 0;
            backButton.Click += (s, e) => { GoBackToWelcome(); };

            serverPanel.Controls.Add(titleLabel);
            serverPanel.Controls.Add(ipLabel);
            serverPanel.Controls.Add(ipTextBox);
            serverPanel.Controls.Add(portLabel);
            serverPanel.Controls.Add(portTextBox);
            serverPanel.Controls.Add(startServerButton);
            serverPanel.Controls.Add(backButton);

            SwitchPanel(serverPanel);
        }

        private void ShowClientSetupPanel()
        {
            Panel clientPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Label titleLabel = new Label
            {
                Text = "Connect to Server",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            Label serverIpLabel = new Label
            {
                Text = "Server IP Address:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point((800 - 400) / 2, 100),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.MiddleRight
            };

            TextBox serverIpTextBox = new TextBox
            {
                Text = GetLocalIPAddress(),
                Font = new Font("Segoe UI", 10),
                Location = new Point((800 - 400) / 2 + 130, 100),
                Size = new Size(270, 30)
            };

            Label portLabel = new Label
            {
                Text = "Port:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point((800 - 400) / 2, 150),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.MiddleRight
            };

            TextBox portTextBox = new TextBox
            {
                Text = "5050",
                Font = new Font("Segoe UI", 10),
                Location = new Point((800 - 400) / 2 + 130, 150),
                Size = new Size(270, 30)
            };

            Button connectButton = new Button
            {
                Text = "Connect",
                Size = new Size(200, 50),
                Location = new Point((800 - 200) / 2, 220),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                FlatStyle = FlatStyle.Flat
            };
            connectButton.FlatAppearance.BorderSize = 0;
            connectButton.Click += (s, e) =>
            {
                int port;
                if (!int.TryParse(portTextBox.Text, out port))
                {
                    MessageBox.Show("Please enter a valid port number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                isServer = false;
                if (ConnectToServer(serverIpTextBox.Text, port))
                {
                    ShowChatPanel();
                }
            };

            Button backButton = new Button
            {
                Text = "Back",
                Size = new Size(100, 40),
                Location = new Point((800 - 100) / 2, 300),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat
            };
            backButton.FlatAppearance.BorderSize = 0;
            backButton.Click += (s, e) => { GoBackToWelcome(); };

            clientPanel.Controls.Add(titleLabel);
            clientPanel.Controls.Add(serverIpLabel);
            clientPanel.Controls.Add(serverIpTextBox);
            clientPanel.Controls.Add(portLabel);
            clientPanel.Controls.Add(portTextBox);
            clientPanel.Controls.Add(connectButton);
            clientPanel.Controls.Add(backButton);

            SwitchPanel(clientPanel);
        }

        private void ShowChatPanel()
        {
            Panel chatPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(10)
            };

            // Top panel with connection info
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = isServer ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 150, 136),
                Padding = new Padding(10, 0, 10, 0)
            };

            Label statusLabel = new Label
            {
                Text = isServer ? "Server Running" : "Connected to Server",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 13),
                AutoSize = true
            };

            Label userLabel = new Label
            {
                Text = $"Username: {username}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(this.Width - 200, 15),
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            topPanel.Controls.Add(statusLabel);
            topPanel.Controls.Add(userLabel);

            // Chat history panel with border
            Panel chatHistoryPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 10, 0, 10)
            };

            RichTextBox chatHistoryBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(5),
                ForeColor = Color.FromArgb(60, 60, 60),
                DetectUrls = true
            };

            // Add a welcome message
            chatHistoryBox.Text = "Welcome to Alrawi Chat!" + Environment.NewLine;
            chatHistoryBox.SelectionStart = 0;
            chatHistoryBox.SelectionLength = chatHistoryBox.Text.Length;
            chatHistoryBox.SelectionFont = new Font("Segoe UI", 12, FontStyle.Bold);
            chatHistoryBox.SelectionColor = Color.FromArgb(0, 150, 136);
            chatHistoryBox.SelectionLength = 0;
            chatHistoryBox.AppendText(Environment.NewLine);
            chatHistoryBox.AppendText("[SYSTEM] Chat session started at " + DateTime.Now.ToString() + Environment.NewLine);

            chatHistoryPanel.Controls.Add(chatHistoryBox);

            // Bottom panel with message input
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10, 0, 10, 10)
            };

            TextBox messageBox = new TextBox
            {
                Location = new Point(10, 15),
                Size = new Size(this.Width - 130, 35),
                Font = new Font("Segoe UI", 11),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5),
                BackColor = Color.White
            };
            messageBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(messageBox.Text))
                    {
                        SendMessageToChat(messageBox.Text);
                        messageBox.Clear();
                    }
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            Button sendButton = new Button
            {
                Text = "Send",
                Location = new Point(this.Width - 110, 15),
                Size = new Size(90, 35),
                BackColor = isServer ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand
            };
            sendButton.FlatAppearance.BorderSize = 0;
            sendButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(messageBox.Text))
                {
                    SendMessageToChat(messageBox.Text);
                    messageBox.Clear();
                }
            };

            bottomPanel.Controls.Add(messageBox);
            bottomPanel.Controls.Add(sendButton);

            // Add controls to chat panel
            chatPanel.Controls.Add(chatHistoryPanel);
            chatPanel.Controls.Add(topPanel);
            chatPanel.Controls.Add(bottomPanel);

            // Save chat history box reference for updates
            chatPanel.Tag = chatHistoryBox;

            SwitchPanel(chatPanel);

            // Set focus to message box
            messageBox.Focus();
        }
        private void SwitchPanel(Panel newPanel)
        {
            Panel currentPanel = this.Tag as Panel;
            if (currentPanel != null)
            {
                this.Controls.Remove(currentPanel);
                currentPanel.Dispose();
            }

            this.Controls.Add(newPanel);
            this.Tag = newPanel;
        }

        private void GoBackToWelcome()
        {
            // Clean up any connections before going back
            CleanupConnections();

            // Remove current panel first
            Panel currentPanel = this.Tag as Panel;
            if (currentPanel != null)
            {
                this.Controls.Remove(currentPanel);
                currentPanel.Dispose();
            }

            // Initialize welcome panel again
            InitializeWelcomePanel();
        }

        #endregion

        #region Network Methods

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        private void StartServer(string ip, int port)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(endPoint);
                server.Listen(5);

                // Start thread to accept clients
                listenThread = new Thread(AcceptClients);
                listenThread.IsBackground = true;
                listenThread.Start();

                AppendToChatHistory($"[SYSTEM] Server started on {ip}:{port}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AcceptClients()
        {
            try
            {
                while (true)
                {
                    Socket clientSocket = server.Accept();
                    IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;

                    // Add client to list
                    lock (clients)
                    {
                        clients.Add(clientSocket);
                        addrs.Add(clientEndPoint);
                    }

                    // Create a new thread for this client
                    Thread clientThread = new Thread(() => HandleClient(clientSocket, clientEndPoint));
                    clientThread.IsBackground = true;
                    clientThread.Start();

                    AppendToChatHistory($"[SYSTEM] New client connected: {clientEndPoint}");
                }
            }
            catch (Exception ex)
            {
                if (!(ex is ObjectDisposedException))
                {
                    AppendToChatHistory($"[ERROR] Server error: {ex.Message}");
                }
            }
        }

        private void HandleClient(Socket clientSocket, IPEndPoint clientEndPoint)
        {
            byte[] buffer = new byte[HEADER];

            try
            {
                while (true)
                {
                    int msgLength = clientSocket.Receive(buffer, 0, HEADER, SocketFlags.None);
                    if (msgLength > 0)
                    {
                        byte[] msgBuffer = new byte[msgLength];
                        clientSocket.Receive(msgBuffer, 0, msgLength, SocketFlags.None);
                        string msg = Encoding.UTF8.GetString(msgBuffer);

                        if (msg == DISCONNECT_MESSAGE)
                            break;

                        AppendToChatHistory($"{msg}");
                        Broadcast(clientEndPoint, msg);
                    }
                }
            }
            catch
            {
                // Client disconnected or error occurred
            }
            finally
            {
                lock (clients)
                {
                    int index = clients.IndexOf(clientSocket);
                    if (index >= 0)
                    {
                        clients.RemoveAt(index);
                        addrs.RemoveAt(index);
                    }
                }

                clientSocket.Close();
                AppendToChatHistory($"[SYSTEM] Client disconnected: {clientEndPoint}");
            }
        }

        private void Broadcast(IPEndPoint senderEndPoint, string message)
        {
            lock (clients)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (addrs[i].Equals(senderEndPoint)) continue;

                    try
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        clients[i].Send(data);
                    }
                    catch
                    {
                        // Will be cleaned up by the client handler thread
                    }
                }
            }
        }

        private bool ConnectToServer(string serverIP, int port)
        {
            try
            {
                client = new TcpClient();
                client.Connect(serverIP, port);
                clientStream = client.GetStream();

                // Start thread to receive messages
                messageThread = new Thread(ReceiveMessages);
                messageThread.IsBackground = true;
                messageThread.Start();

                AppendToChatHistory($"[SYSTEM] Connected to server at {serverIP}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[2048];

            try
            {
                while (true)
                {
                    int bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Connection closed

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AppendToChatHistory(message);
                }
            }
            catch (Exception)
            {
                // Connection lost or closed
                AppendToChatHistory("[SYSTEM] Connection to server lost.");
            }
        }

        private void SendMessageToChat(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (isServer)
            {
                // If we're the server, broadcast message to all clients
                string fullMessage = $"[SERVER] {username}: {message}";
                AppendToChatHistory(fullMessage);
                Broadcast(new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 0), fullMessage);
            }
            else if (client != null && client.Connected)
            {
                // If we're a client, send to server
                string fullMessage = $"{username}: {message}";

                try
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(fullMessage + "\n");

                    // Create header
                    string header = messageBytes.Length.ToString();
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header.PadRight(HEADER));

                    // Send header and message
                    clientStream.Write(headerBytes, 0, headerBytes.Length);
                    clientStream.Write(messageBytes, 0, messageBytes.Length);

                    // Show our message in chat
                    AppendToChatHistory($"You: {message}");
                }
                catch (Exception)
                {
                    AppendToChatHistory("[ERROR] Failed to send message. Connection may be lost.");
                }
            }
        }

        #endregion

        #region Helper Methods

        private void AppendToChatHistory(string message)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action<string>(AppendToChatHistory), message);
                    return;
                }
                catch (Exception)
                {
                    return;
                }
                
            }

            Panel currentPanel = this.Tag as Panel;
            if (currentPanel != null)
            {
                RichTextBox chatBox = currentPanel.Tag as RichTextBox;
                if (chatBox != null)
                {
                    // Determine message type
                    Color messageColor;
                    bool isBold = false;

                    if (message.StartsWith("[SYSTEM]"))
                    {
                        messageColor = Color.DarkGray;
                        isBold = true;
                    }
                    else if (message.StartsWith("[SERVER]"))
                    {
                        messageColor = Color.FromArgb(0, 120, 215);
                        isBold = true;
                    }
                    else if (message.StartsWith("[ERROR]"))
                    {
                        messageColor = Color.Red;
                        isBold = true;
                    }
                    else if (message.StartsWith("You:"))
                    {
                        messageColor = Color.FromArgb(0, 150, 136);
                        message = "► " + message;
                    }
                    else
                    {
                        messageColor = Color.FromArgb(50, 50, 50);
                        message = "► " + message;
                    }

                    // Store the current position
                    int startPos = chatBox.TextLength;

                    // Add text
                    chatBox.AppendText(message + Environment.NewLine);

                    // Select the added text and modify its properties
                    chatBox.Select(startPos, message.Length);
                    chatBox.SelectionColor = messageColor;

                    if (isBold)
                        chatBox.SelectionFont = new Font(chatBox.Font, FontStyle.Bold);

                    // Add a separator line for system messages
                    if (message.StartsWith("[SYSTEM]") || message.StartsWith("[ERROR]"))
                    {
                        int linePos = chatBox.TextLength;
                        chatBox.AppendText("─────────────────────────────────" + Environment.NewLine);
                        chatBox.Select(linePos, 35);
                        chatBox.SelectionColor = Color.LightGray;
                    }

                    // Reset the selection
                    chatBox.SelectionStart = chatBox.TextLength;
                    chatBox.SelectionLength = 0;

                    // Scroll to see the latest message
                    chatBox.ScrollToCaret();
                }
            }
        }
        private void CleanupConnections()
        {
            // Clean up server
            if (server != null)
            {
                try
                {
                    server.Close();
                }
                catch { }
                server = null;
            }

            // Clean up client
            if (client != null)
            {
                try
                {
                    if (client.Connected)
                    {
                        if (clientStream != null)
                        {
                            // Send disconnect message
                            string fullMessage = $"{username}: {DISCONNECT_MESSAGE}\n";
                            byte[] messageBytes = Encoding.UTF8.GetBytes(fullMessage);
                            string header = messageBytes.Length.ToString();
                            byte[] headerBytes = Encoding.UTF8.GetBytes(header.PadRight(HEADER));
                            clientStream.Write(headerBytes, 0, headerBytes.Length);
                            clientStream.Write(messageBytes, 0, messageBytes.Length);

                            clientStream.Close();
                        }
                        client.Close();
                    }
                }
                catch { }
                client = null;
                clientStream = null;
            }

            // Abort threads
            if (listenThread != null && listenThread.IsAlive)
            {
                try { listenThread.Abort(); } catch { }
                listenThread = null;
            }

            if (messageThread != null && messageThread.IsAlive)
            {
                try { messageThread.Abort(); } catch { }
                messageThread = null;
            }

            lock (clients)
            {
                clients.Clear();
                addrs.Clear();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupConnections();
        }
        #endregion
    }

}