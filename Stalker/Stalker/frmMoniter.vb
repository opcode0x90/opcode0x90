Public Class frmMoniter

    Private IRCBot As IRCBot = New IRCBot
    Private WithEvents IRC As IRC = IRCBot.IRC

    Delegate Sub _PrintConsole(ByVal Text As String)
    Delegate Sub _Connected(ByVal Server As String)
    Delegate Sub _Disconnected()

    Private Sub Connected(ByVal Server As String)
        'Connected
        txtNetwork.Enabled = False
        btnConnect.Enabled = False
        btnDisconnect.Enabled = True

        PrintConsole("Connected to " & Server)
    End Sub

    Private Sub Disconnected()
        'Disconnected
        PrintConsole("Disconnected from " & txtNetwork.Text)

        txtNetwork.Enabled = True
        btnConnect.Enabled = True
        btnDisconnect.Enabled = False
    End Sub

    Private Sub PrintConsole(ByVal Text As String)
        'Is this a cross-thread call ?
        If txtConsole.InvokeRequired Then
            Dim callback As _PrintConsole = New _PrintConsole(AddressOf PrintConsole)

            'Invoke the callback
            Me.Invoke(callback, New Object() {Text})
        Else
            'Print the text to console
            txtConsole.Text += (Text & vbCrLf)
        End If

    End Sub

    Private Sub btnShutdown_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnShutdown.Click
        IRCBot.Disconnect()
        Application.Exit()
    End Sub

    Private Sub btnConnect_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnConnect.Click
        IRCBot.Connect()
    End Sub

    Private Sub btnDisconnect_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDisconnect.Click
        IRCBot.Disconnect()
    End Sub

    Private Sub IRC_OnChannelJoin(ByVal Channel As String) Handles IRC.OnChannelJoin
        'Joined the channel
        PrintConsole("Joined channel " & Channel)
    End Sub

    Private Sub IRC_OnChannelPart(ByVal Channel As String) Handles IRC.OnChannelPart
        'Left the channel
        PrintConsole("Left channel " & Channel)
    End Sub

    Private Sub IRC_OnChannelUserJoin(ByVal Channel As String, ByVal Nick As String) Handles IRC.OnChannelUserJoin
        'Somebody is on
        PrintConsole("User " & Nick & " joined the channel " & Channel)
    End Sub

    Private Sub IRC_OnChannelUserPart(ByVal Channel As String, ByVal Nick As String, ByVal Message As String) Handles IRC.OnChannelUserPart
        'Somebody left
        PrintConsole("User " & Nick & " left the channel " & Channel)
    End Sub

    Private Sub IRC_OnConnect(ByVal Server As String) Handles IRC.OnConnect
        'Connected to the IRC network
        Dim callback As _Connected = New _Connected(AddressOf Connected)
        Me.Invoke(callback, New Object() {Server})
    End Sub

    Private Sub IRC_OnCTCP(ByVal Nick As String, ByVal CTCP As String, ByVal Params As String) Handles IRC.OnCTCP
        'Received some annoying CTCP
        PrintConsole("Received CTCP " & CTCP & " from user " & Nick)
    End Sub

    Private Sub IRC_OnDisconnect() Handles IRC.OnDisconnect
        'Disconnected from the IRC network
        Dim callback As _Disconnected = New _Disconnected(AddressOf Disconnected)
        Me.Invoke(callback)
    End Sub

    Private Sub IRCBot_OnException(ByVal ex As System.Exception) Handles IRC.OnException
        'Exception occurred
        PrintConsole(ex.Message)
    End Sub

    Private Sub btnSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSend.Click
        'Send the specified message
        IRCBot.IRC.Send(txtMessage.Text)
        txtMessage.ResetText()
    End Sub

End Class
