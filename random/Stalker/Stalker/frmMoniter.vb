Public Class frmMoniter

    Private WithEvents IRCBot As IRCBot = New IRCBot
    Private WithEvents IRC As IRC = IRCBot.IRC

    Delegate Sub _PrintConsole(ByVal Text As String)
    Delegate Sub _Connected(ByVal Server As String)
    Delegate Sub _Disconnected()
    Delegate Sub _Exception(ByVal ex As Exception)

    Private Sub Connected(ByVal Server As String)
        'Is this a cross-thread call ?
        If InvokeRequired Then
            'Invoke the callback
            Invoke(New _Connected(AddressOf Connected), New Object() {Server})
        Else
            'Connected
            txtMessage.Enabled = True
            btnSend.Enabled = True

            'Notify
            PrintConsole("Connected to " & Server)
            NotifyIcon.ShowBalloonTip(5000, "StalkerBot", "Connected to " & Server, ToolTipIcon.Info)
        End If
    End Sub

    Private Sub Disconnected()
        'Is this a cross-thread call ?
        If InvokeRequired Then
            'Invoke the callback
            Invoke(New _Disconnected(AddressOf Disconnected))
        Else
            'Disconnected
            PrintConsole("Disconnected from " & IRC.Server)
            NotifyIcon.ShowBalloonTip(5000, "StalkerBot", "Disconnected from " & IRC.Server, ToolTipIcon.Warning)

            txtMessage.Enabled = False
            btnSend.Enabled = False
        End If
    End Sub

    Private Sub ExceptionOccurred(ByVal ex As Exception)
        'Is this a cross-thread call ?
        If InvokeRequired Then
            'Invoke the callback
            Invoke(New _Exception(AddressOf ExceptionOccurred), New Object() {ex})
        Else
            'Exception occurred
            PrintConsole("***** EXCEPTION DUMP BEGIN *****")
            PrintConsole(ex.Message)
            PrintConsole(String.Empty)
            PrintConsole(ex.StackTrace)
            PrintConsole(String.Empty)
            PrintConsole("*****  EXCEPTION DUMP END  *****")

            'Notify
            NotifyIcon.ShowBalloonTip(5000, "StalkerBot", "Exception occurred", ToolTipIcon.Error)
        End If
    End Sub

    Public Sub PrintConsole(ByVal Text As String)
        'Is this a cross-thread call ?
        If InvokeRequired Then
            Dim callback As _PrintConsole = New _PrintConsole(AddressOf PrintConsole)

            'Invoke the callback
            Invoke(callback, New Object() {Text})
        Else
            'Print the text to console
            txtConsole.AppendText(Text & vbCrLf)
            txtConsole.SelectionStart = txtConsole.TextLength
            Application.DoEvents()

            'Dump it to stdout and debug too
            Console.WriteLine(Text)
            Debug.Print(Text)
        End If

    End Sub

    Private Sub btnShutdown_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnShutdown.Click
        IRCBot.Kill()
        Application.Exit()
    End Sub

    Private Sub IRC_OnChannelJoin(ByVal Channel As Channel, ByVal User As User) Handles IRC.OnChannelJoin
        'Somebody is on
        PrintConsole("*** User " & User.Nick & " joined the channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnChannelPart(ByVal Channel As Channel, ByVal User As User, ByVal Message As String) Handles IRC.OnChannelPart
        'Somebody left
        PrintConsole("*** User " & User.Nick & " left the channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnConnect(ByVal Server As String) Handles IRC.OnConnect
        'Connected to the IRC network
        Connected(Server)
    End Sub

    Private Sub IRC_OnCTCP(ByVal User As User, ByVal CTCP As String, ByVal Params As String) Handles IRC.OnCTCP
        'Received some annoying CTCP
        PrintConsole("*** Received CTCP " & CTCP & " from user " & User.Nick)
    End Sub

    Private Sub IRC_OnDisconnect() Handles IRC.OnDisconnect
        'Disconnected from the IRC network
        Disconnected()
    End Sub

    Private Sub IRC_OnException(ByVal ex As System.Exception) Handles IRC.OnException
        'Exception occurred
        ExceptionOccurred(ex)
    End Sub

    Private Sub btnSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSend.Click
        'Send the specified message
        IRCBot.IRC.Send(txtMessage.Text)
        txtMessage.ResetText()
    End Sub

    Private Sub IRCBot_OnException(ByVal ex As System.Exception) Handles IRCBot.OnException
        'Exception occurred
        ExceptionOccurred(ex)
    End Sub

    Private Sub IRC_OnJoin(ByVal Channel As Channel) Handles IRC.OnJoin
        'Joined the channel
        PrintConsole("*** Joined channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnNickChange(ByVal OldNick As String, ByVal NewNick As String) Handles IRC.OnNickChange
        'Changed our nick
        PrintConsole("*** Nick changed from " & OldNick & " to " & NewNick)
    End Sub

    Private Sub IRC_OnPart(ByVal Channel As Channel) Handles IRC.OnPart
        'Left the channel
        PrintConsole("*** Left channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnRawMessage(ByVal Message As String) Handles IRC.OnRawMessage
        'Dump it to console for the lulz
        PrintConsole("<< " + Message)
    End Sub

    Private Sub IRC_OnRawMessageSent(ByVal Message As String) Handles IRC.OnRawMessageSent
        'Dump it to console for the lulz
        PrintConsole(">> " + Message)
    End Sub

    Private Sub ShutdownToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ShutdownToolStripMenuItem.Click
        btnShutdown_Click(Nothing, Nothing)
    End Sub

    Private Sub ShowToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ShowToolStripMenuItem.Click
        Me.Show()
    End Sub

    Private Sub btnHide_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnHide.Click
        Me.Hide()
    End Sub

    Private Sub NotifyIcon_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles NotifyIcon.DoubleClick
        Me.Show()
    End Sub

    Private Sub AutoConnectTimer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AutoConnectTimer.Tick
        'Auto connect if not connected
        If Not IRC.Connected Then IRCBot.Start()
    End Sub

    Private Sub frmMoniter_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        IRCBot.Start()
    End Sub

End Class
