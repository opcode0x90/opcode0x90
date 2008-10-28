Public Class IRCBot

    Public Network As String = "irc.p2p-network.net"
    Public Port As Integer = 6667
    Public Nick As String = "noppy"
    Public NickPassword As String = "seriouslysecurepassword"
    Public User As String = "I r stalker"
    Public Channel As String = "#baw"
    Public LogChannel As String = "#emptylog" '"#cef-loginfo"

    'IRC client
    Public WithEvents IRC As IRC = New IRC

    Public Sub Connect()
        'Connect to the IRC network
        IRC.Connect(Network, Port, Nick, User)
    End Sub

    Public Sub Disconnect()
        'Disconnect from the IRC network
        IRC.Disconnect()
    End Sub

    Private Sub IRC_OnChannelKick(ByVal Channel As Channel, ByVal User As User, ByVal Nick As String, ByVal Reason As String) Handles IRC.OnChannelKick
        'Who kick mah ass ? >:|
        IRC.ChannelJoin(Channel.Name)
        IRC.Message(Channel.Name, "Who kick mah ass >:|")
    End Sub

    Private Sub IRC_OnChannelMessage(ByVal Channel As Channel, ByVal User As User, ByVal Message As String) Handles IRC.OnChannelMessage
        'Listen only message from the log channel
        If (Channel.Name = LogChannel) Then
            'Is this a command ?
            If Message.StartsWith("!") Then
                'Handle command here
            End If
        End If

    End Sub

    Private Sub IRC_OnChannelUserJoin(ByVal Channel As Channel, ByVal User As User) Handles IRC.OnChannelUserJoin
        'Log the user join
        IRC.Message(LogChannel, "Nick: " + User.Nick + " | Mask: " + (User.Ident + "@" + User.Host) + " | Real Name: " + User.RealName)
    End Sub

    Private Sub IRC_OnConnect(ByVal Server As String) Handles IRC.OnConnect
        'Identify
        IRC.Identify(NickPassword)

        'Autojoin channel
        IRC.ChannelJoin(Channel)
        IRC.ChannelJoin(LogChannel)
    End Sub

    Private Sub IRC_OnCTCP(ByVal User As User, ByVal CTCP As String, ByVal Params As String) Handles IRC.OnCTCP
        'Debug
        IRC.Message(LogChannel, "Received CTCP " + CTCP + IIf(String.IsNullOrEmpty(Params), String.Empty, (" " + Params)) + " from " + User.Mask)

        Select Case CTCP.ToUpper
            Case "PING"
                'Pong
                IRC.NCTCP(User.Nick, "PING", Params)
            Case "VERSION"
                'I r stalker
                IRC.NCTCP(User.Nick, "VERSION", "StalkerBot")
        End Select

    End Sub

End Class
