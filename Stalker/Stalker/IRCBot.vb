Public Class IRCBot

    Public Network As String = "irc.p2p-network.net"
    Public Port As Integer = 6667
    Public Nick As String = "noppy"
    Public NickPassword As String = "seriouslysecurepassword"
    Public User As String = "I r stalker"
    Public Channel As String = "#cef"
    Public LogChannel As String = "#cef-loginfo"

    'IRC client
    Public WithEvents IRC As IRC = New IRC

    Public Sub Connect()
        'Connect to the IRC network
        IRC.Connect(Me.Network, Me.Port, Me.Nick, Me.User)
    End Sub

    Public Sub Disconnect()
        'Disconnect from the IRC network
        IRC.Disconnect()
    End Sub

    Private Sub IRC_OnChannelMessage(ByVal Channel As String, ByVal Nick As String, ByVal Message As String) Handles IRC.OnChannelMessage
        'Listen only message from the log channel
        If (Channel = Me.LogChannel) Then
            'Is this a command ?
            If Message.StartsWith("!") Then
                'Handle command here
            End If
        End If

    End Sub

    Private Sub IRC_OnChannelUserJoin(ByVal Channel As String, ByVal Nick As String) Handles IRC.OnChannelUserJoin
        'Log the user join
        IRC.Message(Me.LogChannel, "Mask: " + Nick)
    End Sub

    Private Sub IRC_OnConnect(ByVal Server As String) Handles IRC.OnConnect
        'Identify
        IRC.Identify(Me.NickPassword)

        'Autojoin channel
        IRC.ChannelJoin(Me.Channel)
        IRC.ChannelJoin(Me.LogChannel)
    End Sub

    Private Sub IRC_OnCTCP(ByVal Nick As String, ByVal CTCP As String, ByVal Params As String) Handles IRC.OnCTCP
        'Debug
        IRC.Message(Me.LogChannel, "Received CTCP " + CTCP + IIf(String.IsNullOrEmpty(Params), String.Empty, (" " + Params)) + " from " + Nick)

        Select Case CTCP.ToUpper
            Case "PING"
                'Pong
                IRC.Notify(Nick, "PING " + Params)
            Case "VERSION"
                'I r stalker
                IRC.Notify(Nick, "VERSION StalkerBot")
        End Select

    End Sub

    Private Sub IRC_OnKick(ByVal Nick As String, ByVal Channel As String, ByVal Reason As String) Handles IRC.OnKick
        'Who kick mah ass ? >:|
        IRC.ChannelJoin(Channel)
        IRC.Message(Channel, "Who kick mah ass >:|")
    End Sub

End Class
