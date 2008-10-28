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
        PrintConsole("Disconnected from " & IRC.Server)

        txtNetwork.Enabled = True
        btnConnect.Enabled = True
        btnDisconnect.Enabled = False
    End Sub

    Private Sub PrintConsole(ByVal Text As String)
        'Is this a cross-thread call ?
        If txtConsole.InvokeRequired Then
            Dim callback As _PrintConsole = New _PrintConsole(AddressOf PrintConsole)

            'Invoke the callback
            Invoke(callback, New Object() {Text})
        Else
            'Print the text to console
            txtConsole.AppendText(Text & vbCrLf)
            txtConsole.SelectionStart = txtConsole.TextLength
        End If

    End Sub

    Private Sub btnShutdown_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnShutdown.Click
        IRCBot.Disconnect()
        Application.Exit()
    End Sub

    Private Sub btnConnect_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnConnect.Click
        txtNetwork.Enabled = False
        btnConnect.Enabled = False
        btnDisconnect.Enabled = True
        Application.DoEvents()

        IRCBot.Connect()
    End Sub

    Private Sub btnDisconnect_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDisconnect.Click
        IRCBot.Disconnect()
    End Sub

    Private Sub IRC_OnChannelJoin(ByVal Channel As Channel) Handles IRC.OnChannelJoin
        'Joined the channel
        PrintConsole("Joined channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnChannelPart(ByVal Channel As Channel) Handles IRC.OnChannelPart
        'Left the channel
        PrintConsole("Left channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnChannelUserJoin(ByVal Channel As Channel, ByVal User As User) Handles IRC.OnChannelUserJoin
        'Somebody is on
        PrintConsole("User " & User.Nick & " joined the channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnChannelUserPart(ByVal Channel As Channel, ByVal User As User, ByVal Message As String) Handles IRC.OnChannelUserPart
        'Somebody left
        PrintConsole("User " & User.Nick & " left the channel " & Channel.Name)
    End Sub

    Private Sub IRC_OnConnect(ByVal Server As String) Handles IRC.OnConnect
        If InvokeRequired Then
            'Connected to the IRC network
            Dim callback As _Connected = New _Connected(AddressOf Connected)
            Invoke(callback, New Object() {Server})
        End If
    End Sub

    Private Sub IRC_OnCTCP(ByVal User As User, ByVal CTCP As String, ByVal Params As String) Handles IRC.OnCTCP
        'Received some annoying CTCP
        PrintConsole("Received CTCP " & CTCP & " from user " & User.Nick)
    End Sub

    Private Sub IRC_OnDisconnect() Handles IRC.OnDisconnect
        If InvokeRequired Then
            'Disconnected from the IRC network
            Dim callback As _Disconnected = New _Disconnected(AddressOf Disconnected)
            Invoke(callback)
        End If
    End Sub

    Private Sub IRCBot_OnException(ByVal ex As System.Exception) Handles IRC.OnException
        'Exception occurred
        PrintConsole("***** EXCEPTION DUMP BEGIN *****")
        PrintConsole(ex.Message)
        PrintConsole(String.Empty)
        PrintConsole(ex.StackTrace)
        PrintConsole(String.Empty)
        PrintConsole("*****  EXCEPTION DUMP END  *****")
    End Sub

    Private Sub btnSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSend.Click
        'Send the specified message
        IRCBot.IRC.Send(txtMessage.Text)
        txtMessage.ResetText()
    End Sub

End Class
