
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using AIStudyPlannerApp.Backend;
using AIStudyPlannerApp.Frontend.Controls;

namespace AIStudyPlannerApp.Frontend.Pages
{
    public class AIChatPage : UserControl
    {
        private readonly IAIService aiService = new AIService();
        private ChatHistoryService? _chatHistoryService;
        private const int CurrentUserId = 1;

        private readonly List<(string Role, string Content)> history = new();

        /// Creates and returns the ChatHistoryService used to load and save chat messages
        /// Uses a cached instance for the lifetime of this page
     
        private ChatHistoryService GetChatHistoryService()
        {
            _chatHistoryService ??= new ChatHistoryService();
            return _chatHistoryService;
        }

        private readonly FlowLayoutPanel chatPanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(248, 249, 252)
        };

        private readonly TextBox inputBox = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            Height = 60,
            Font = new Font("Segoe UI", 11),
            BorderStyle = BorderStyle.FixedSingle
        };

        private readonly Button sendButton = new()
        {
            Text = "Send",
            Dock = DockStyle.Right,
            Width = 100,
            BackColor = Color.FromArgb(52, 120, 246),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        private bool isSending;

        private sealed class MessageView
        {
            public Panel Row { get; set; } = null!;
            public Label? Label { get; set; }
            public RichTextBox? RichTextBox { get; set; }
            public Control? Bubble { get; set; }
            public bool IsUser { get; set; }
        }

        private readonly List<MessageView> messageViews = new();
        private bool isLayingOutMessages;



        public AIChatPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;

            BuildLayout();
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await LoadHistoryAsync();
        }

        /// Builds the page UI, including the chat area and input area
        /// Wires up the Send button, Enter key, and chat panel resize handler.
        /// This method configures controls and their events
        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 0)
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // header
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // chat area
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88)); // input area

            Controls.Add(layout);


            // Chat area (FlowLayoutPanel)
            chatPanel.FlowDirection = FlowDirection.TopDown;
            chatPanel.WrapContents = false;
            chatPanel.AutoScroll = true;
            chatPanel.Padding = new Padding(12);
            chatPanel.Dock = DockStyle.Fill;
            layout.Controls.Add(chatPanel, 0, 1);

      
            var inputRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16, 0, 16, 12)
            };
            inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Rounded-like input container 
            var inputContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(10),
                Height = 56,
                Margin = Padding.Empty
            };
            inputBox.Multiline = true;
            inputBox.BorderStyle = BorderStyle.None;
            inputBox.Font = new Font("Segoe UI", 11);
            inputBox.AcceptsReturn = true;
            inputBox.Height = 36;
            inputBox.Dock = DockStyle.Fill;
            inputBox.Margin = Padding.Empty;
            inputContainer.Controls.Add(inputBox);

            sendButton.Width = 100;
            sendButton.Height = 40;
            sendButton.FlatStyle = FlatStyle.Flat;
            sendButton.FlatAppearance.BorderSize = 0;
            sendButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            sendButton.BackColor = Color.FromArgb(52, 120, 246);
            sendButton.ForeColor = Color.White;
            sendButton.Margin = new Padding(12, 0, 0, 0);

            inputRow.Controls.Add(inputContainer, 0, 0);
            inputRow.Controls.Add(sendButton, 1, 0);

            layout.Controls.Add(inputRow, 0, 2);

            // Events
            sendButton.Click += async (_, _) => await SendMessageAsync();

            inputBox.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    await SendMessageAsync();
                }
            };

            // Ensure chat bubbles get resized when panel resizes

            chatPanel.SizeChanged += (_, _) => LayoutAllMessages();
        }




        /// Creates and appends a message row to the chat panel
        /// User messages are displayed in a right-aligned rounded bubble using a label
        /// Returns a MessageView containing references to the controls for later updates
        private MessageView AddMessageView(string message, string role, bool isUser)
        {
            var row = new Panel
            {
                AutoSize = false,
                Width = GetMessageAreaWidth(),
                Height = 40,
                Margin = new Padding(0, 0, 0, 8),
                Padding = Padding.Empty
            };

            int maxWidth = Math.Max(240, chatPanel.ClientSize.Width - chatPanel.Padding.Horizontal - 80);
            int bubbleMax = Math.Min(900, maxWidth);

            if (isUser)
            {
                var bubble = new RoundedPanel
                {
                    AutoSize = false,
                    BackColor = Color.FromArgb(230, 244, 255),
                    Padding = new Padding(10),
                    Margin = new Padding(8, 8, 8, 8)
                };

                var lbl = new Label
                {
                    AutoSize = false,
                    Text = message,
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(20, 40, 60),
                    BackColor = Color.Transparent
                };

                bubble.Controls.Add(lbl);


                row.Controls.Add(bubble);
                chatPanel.Controls.Add(row);

                var mv = new MessageView
                {
                    Row = row,
                    Label = lbl,
                    RichTextBox = null,
                    Bubble = bubble,
                    IsUser = true
                };

                messageViews.Add(mv);
                LayoutMessage(mv);
                ScrollToBottom();
                return mv;
            }
            else
            {
                var bubble = new Panel
                {
                    AutoSize = false,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0),
                    Margin = new Padding(8),
                    MaximumSize = new Size(bubbleMax, 0)
                };

                var rtb = new RichTextBox
                {
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(248, 249, 252),
                    DetectUrls = true,
                    ScrollBars = RichTextBoxScrollBars.None,
                    Font = new Font("Segoe UI", 10),
                    Margin = Padding.Empty,
                    Width = Math.Min(bubbleMax, Math.Max(240, chatPanel.ClientSize.Width - chatPanel.Padding.Horizontal - 80)),
                    WordWrap = true
                };

                rtb.Text = message;

                bubble.Controls.Add(rtb);

                row.Controls.Add(bubble);
                chatPanel.Controls.Add(row);

                var mv = new MessageView
                {
                    Row = row,
                    Label = null,
                    RichTextBox = rtb,
                    Bubble = bubble,
                    IsUser = false
                };

                messageViews.Add(mv);
                RenderMarkdownIntoRichTextBox(rtb, message);
                LayoutMessage(mv);
                ScrollToBottom();

                return mv;
            }
        }

        /// Renders a limited subset of Markdown in the provided RichTextBox, including
        /// headings (# and ##), bold text, unordered lists, and fenced code blocks
        /// Updates the RichTextBox text and applies formatting to selected text ranges
        private void RenderMarkdownIntoRichTextBox(RichTextBox rtb, string message)
        {
            if (rtb == null) return;

            // Normalize
            string text = message.Replace("\r\n", "\n").TrimEnd() + "\n";
            rtb.Clear();
            rtb.SuspendLayout();

            // We'll build the plain text then apply formatting spans
            var spans = new List<(int start, int len, Action<RichTextBox> apply)>();
            var sb = new StringBuilder();

            int pos = 0;

            using (var reader = new System.IO.StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("```"))
                    {
                        var codeSb = new StringBuilder();
                        while ((line = reader.ReadLine()) != null && !line.StartsWith("```"))
                        {
                            codeSb.AppendLine(line);
                        }
                        string codeText = codeSb.ToString().TrimEnd('\r', '\n') + "\n";
                        int start = pos;
                        sb.Append(codeText);
                        int len = codeText.Length;
                        spans.Add((start, len, r =>
                        {
                            r.SelectionStart = start;
                            r.SelectionLength = len;
                            r.SelectionFont = new Font("Consolas", 9);
                            r.SelectionColor = Color.Black;
                            r.SelectionBackColor = Color.FromArgb(245, 245, 245);
                        }
                        ));
                        pos += len;
                        continue;
                    }

                    // Heading
                    if (line.StartsWith("## "))
                    {
                        string h = line.Substring(3).Trim();
                        int start = pos;
                        sb.Append(h + "\n");
                        spans.Add((start, h.Length + 1, r =>
                        {
                            r.SelectionStart = start;
                            r.SelectionLength = h.Length;
                            r.SelectionFont = new Font(r.SelectionFont.FontFamily, 14, FontStyle.Bold);
                        }
                        ));
                        pos += h.Length + 1;
                        continue;
                    }
                    if (line.StartsWith("# "))
                    {
                        string h = line.Substring(2).Trim();
                        int start = pos;
                        sb.Append(h + "\n");
                        spans.Add((start, h.Length + 1, r =>
                        {
                            r.SelectionStart = start;
                            r.SelectionLength = h.Length;
                            r.SelectionFont = new Font(r.SelectionFont.FontFamily, 16, FontStyle.Bold);
                        }
                        ));
                        pos += h.Length + 1;
                        continue;
                    }

                    // Unordered list
                    if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                        string content = "• " + line.Substring(2).Trim();
                        int start = pos;
                        sb.Append(content + "\n");
                        pos += content.Length + 1;
                        continue;
                    }

                    // Inline bold 
                    int idx = 0;
                    while (true)
                    {
                        int b1 = line.IndexOf("**", idx, StringComparison.Ordinal);
                        if (b1 == -1) break;
                        int b2 = line.IndexOf("**", b1 + 2, StringComparison.Ordinal);
                        if (b2 == -1) break;
                        if (b1 > idx)
                        {
                            sb.Append(line.Substring(idx, b1 - idx));
                            pos += b1 - idx;
                        }
                        string boldText = line.Substring(b1 + 2, b2 - (b1 + 2));
                        int start = pos;
                        sb.Append(boldText);
                        spans.Add((start, boldText.Length, r =>
                        {
                            r.SelectionStart = start;
                            r.SelectionLength = boldText.Length;
                            r.SelectionFont = new Font(r.SelectionFont.FontFamily, r.SelectionFont.Size, FontStyle.Bold);
                        }
                        ));
                        pos += boldText.Length;
                        idx = b2 + 2;
                    }
                    if (idx < line.Length)
                    {
                        string rem = line.Substring(idx);
                        sb.Append(rem);
                        pos += rem.Length;
                    }
                    sb.Append("\n");
                    pos += 1;
                }
            }

            rtb.Text = sb.ToString();

            // Apply spans
            foreach (var span in spans)
            {
                try
                {
                    rtb.Select(span.start, span.len);
                    span.apply(rtb);
                }
                catch { }
            }

            rtb.Select(0, 0);
            rtb.ResumeLayout();
        }

        private int GetMessageAreaWidth()
        {
            int width = chatPanel.ClientSize.Width
                - chatPanel.Padding.Horizontal - 8;

            if (chatPanel.VerticalScroll.Visible)
                width -= SystemInformation.VerticalScrollBarWidth;

            return Math.Max(200, width);
        }


        private void LayoutMessage(MessageView view)
        {
            if (view.Row.IsDisposed)
                return;

            int rowWidth = GetMessageAreaWidth();
            view.Row.Width = rowWidth;

            const TextFormatFlags flags =
                TextFormatFlags.WordBreak |
                TextFormatFlags.TextBoxControl;

            // User message: Label inside a rounded bubble
            if (view.IsUser &&
                view.Label is Label label &&
                view.Bubble is Control userBubble)
            {
                int maxTextWidth = Math.Max(
                    120, Math.Min(700, rowWidth - 64));

                Size naturalSize = TextRenderer.MeasureText(
                    string.IsNullOrEmpty(label.Text) ? " " : label.Text,
                    label.Font,
                    new Size(10000, 100000),
                    flags);

                int textWidth = Math.Max(
                    20, Math.Min(maxTextWidth, naturalSize.Width));

                Size wrappedSize = TextRenderer.MeasureText(
                    string.IsNullOrEmpty(label.Text) ? " " : label.Text,
                    label.Font,
                    new Size(textWidth, 100000),
                    flags);

                label.Size = new Size(
                    textWidth,
                    Math.Max(label.Font.Height + 2, wrappedSize.Height));

                int bubbleWidth = textWidth + 24;
                int bubbleHeight = label.Height + 20;

                userBubble.Size = new Size(bubbleWidth, bubbleHeight);
                label.Location = new Point(12, 10);

                userBubble.Location = new Point(
                    Math.Max(8, rowWidth - bubbleWidth - 8), 4);

                view.Row.Height = bubbleHeight + 8;
            }
            else if (view.RichTextBox is RichTextBox rtb &&
                     view.Bubble is Control assistantContainer)
            {
                int textWidth = Math.Max(
                    200, Math.Min(900, rowWidth - 24));

                rtb.Width = textWidth;

                Size measured = TextRenderer.MeasureText(
                    string.IsNullOrEmpty(rtb.Text) ? " " : rtb.Text,
                    rtb.Font,
                    new Size(Math.Max(100, textWidth - 12), 100000),
                    flags);

                rtb.Height = Math.Max(32, measured.Height + 16);

                assistantContainer.Size = new Size(
                    textWidth, rtb.Height);

                assistantContainer.Location = new Point(8, 4);
                view.Row.Height = rtb.Height + 12;
            }
        }



        /// Recalculates the widths, heights, and positions of all existing message controls
        /// based on the current chat panel size, so text wrapping remains correct after resizing
        private void LayoutAllMessages()
        {
            if (isLayingOutMessages || chatPanel.IsDisposed)
                return;

            isLayingOutMessages = true;
            chatPanel.SuspendLayout();

            try
            {
                foreach (MessageView view in messageViews)
                {
                    if (view.Row != null && !view.Row.IsDisposed)
                        LayoutMessage(view);
                }
            }
            finally
            {
                chatPanel.ResumeLayout(true);
                isLayingOutMessages = false;
            }

            ScrollToBottom();
        }

        /// Scrolls the chat panel so that its last message control is visible
        private void ScrollToBottom()
        {
            if (chatPanel.Controls.Count > 0)
            {
                chatPanel.ScrollControlIntoView(chatPanel.Controls[chatPanel.Controls.Count - 1]);
            }
        }

        private void RefreshMessageLayout(Label label)
        {
            foreach (MessageView view in messageViews)
            {
                if (ReferenceEquals(view.Label, label))
                {
                    LayoutMessage(view);
                    chatPanel.PerformLayout();
                    ScrollToBottom();
                    return;
                }
            }
        }

        /// Sends the current input to the AI service
        /// Adds and saves the user message, displays a Thinking placeholder, requests the AI reply,
        /// replaces the placeholder with the formatted reply, and saves the assistant message
        /// Disables the input controls while sending and displays request errors in the chat
        private async Task SendMessageAsync()
        {
            string message = inputBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(message) || isSending)
                return;

            isSending = true;
            inputBox.Enabled = false;
            sendButton.Enabled = false;

            try
            {
                AddMessageView(message, "You", true);
                history.Add(("user", message));
                await GetChatHistoryService().SaveAsync(CurrentUserId, "user", message);

                inputBox.Clear();
                LayoutAllMessages();
                ScrollToBottom();

                var assistantView = AddMessageView("Thinking...", "AI Study Assistant", false);

                RichTextBox? placeholderRtb = assistantView.RichTextBox;

                ScrollToBottom();

                string? studyContext = await TryBuildStudyContextAsync();
                string reply;
                try
                {
                    reply = await aiService.GetReplyAsync(history, studyContext);
                }
                catch (Exception ex)
                {
                    if (placeholderRtb != null && !placeholderRtb.IsDisposed)
                        placeholderRtb.Text = "Error: " + ex.Message;
                    return;
                }

                if (placeholderRtb != null && !placeholderRtb.IsDisposed)
                {
                    RenderMarkdownIntoRichTextBox(placeholderRtb, reply);
                }
                else
                {
                    // Fallback
                    AddMessageView(reply, "AI Study Assistant", false);
                }

                // Persist assistant reply and update history
                history.Add(("assistant", reply));
                await GetChatHistoryService().SaveAsync(CurrentUserId, "assistant", reply);

                LayoutAllMessages();
                ScrollToBottom();
            }
            finally
            {
                isSending = false;
                inputBox.Enabled = true;
                sendButton.Enabled = true;
                inputBox.Focus();
            }
        }

        // Loads user data as text for the AI, returns null if cant be loaded
        private async Task<string?> TryBuildStudyContextAsync()
        {
            try
            {
                var builder = new StudyContextBuilder();
                return await builder.BuildAsync(CurrentUserId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// Loads saved conversation messages for CurrentUserId and restores both the UI and
        /// in-memory history in database order. Loaded messages are displayed without being saved again
        private async Task LoadHistoryAsync()
        {
            try
            {
                var service = GetChatHistoryService();
                var records = await service.LoadAsync(CurrentUserId);

                history.Clear();
                messageViews.Clear();
                chatPanel.Controls.Clear();

                foreach (var r in records)
                {
                    bool isUser = string.Equals(r.Role, "user", StringComparison.OrdinalIgnoreCase);
                    AddMessageView(r.Content, isUser ? "You" : "AI Study Assistant", isUser);
                    history.Add((isUser ? "user" : "assistant", r.Content));
                }

                if (records.Count == 0)
                {
                    // No DB history: show greeting (do not save)
                    AddMessageView("Hi! I'm your AI study assistant. Ask me about C#, programming, or your study plan.", "AI Study Assistant", false);
                }

                LayoutAllMessages();
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                // show non-fatal error in chat
                AddMessageView("Failed to load chat history: " + ex.Message, "System", false);
            }
        }
    }
}
