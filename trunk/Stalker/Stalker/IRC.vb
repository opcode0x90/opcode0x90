Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Public Class IRC
    'IRC main thread
    Private IrcThread As Thread

    'Socket and streams
    Private Socket As TcpClient
    Private Stream As NetworkStream
    Private Reader As StreamReader
    Private Writer As StreamWriter

    'IRC network variables
    Private _Server As String       'Currently connected IRC network
    Private _Mask As String         'Our own mask
    Private _Nick As String         'Our nickname

    Private Reason As String

    'Joined channel and tracked user list
    Private Channels As Dictionary(Of String, Channel) = New Dictionary(Of String, Channel)

    'IRC Events
    Public Event OnConnect(ByVal Server As String)
    Public Event OnDisconnect(ByVal Reason As String)

    Public Event OnKick(ByVal User As User, ByVal Channel As Channel, ByVal Reason As String)
    Public Event OnCTCP(ByVal User As User, ByVal CTCP As String, ByVal Params As String)
    Public Event OnJoin(ByVal Channel As Channel)
    Public Event OnPart(ByVal Channel As Channel)
    Public Event OnNickChange(ByVal OldNick As String, ByVal NewNick As String)

    Public Event OnChannelKick(ByVal Channel As Channel, ByVal User As User, ByVal Nick As String, ByVal Reason As String)
    Public Event OnChannelMessage(ByVal Channel As Channel, ByVal User As User, ByVal Message As String)
    Public Event OnChannelJoin(ByVal Channel As Channel, ByVal User As User)
    Public Event OnChannelPart(ByVal Channel As Channel, ByVal User As User, ByVal Message As String)

    Public Event OnRawMessage(ByVal Message As String)
    Public Event OnRawMessageSent(ByVal Message As String)
    Public Event OnRawServerAnnounce(ByVal FullHeader As String, ByVal Header As String(), ByVal Message As String)
    Public Event OnRawUserMessage(ByVal FullHeader As String, ByVal Header As String(), ByVal Message As String)

    Public Event OnException(ByVal ex As Exception)

    Private Sub IrcThreadStart()

        'Reset
        Reason = String.Empty

        'The main IRC thread
        While Socket.Connected
            Try
                'Poll for message from the network
                Poll()

            Catch ex As IOException
                'Probably disconnected from the network

                'Catch ex As Exception
                'Exception occurred
                '   RaiseEvent OnException(ex)

            End Try

        End While

        'Close all stream
        Writer.Close()
        Reader.Close()
        Stream.Close()

        'Clear all channel
        Channels.Clear()

        'Disconnected
        RaiseEvent OnDisconnect(Reason)

    End Sub

    Private Sub IrcMessagePoll()

        Dim Header As String()
        Dim FullHeader As String
        Dim Message As String
        Dim Pos As Integer

        'Poll for message from the network
        Dim Buffer As String = Reader.ReadLine()

        'Notify there is event happening on the network
        RaiseEvent OnRawMessage(Buffer)

        If Buffer.StartsWith(":") Then
            'Extract the message header and body
            Pos = Buffer.IndexOf(":", 1)
            FullHeader = Buffer.Substring(1, IIf(Pos > 0, Pos, Buffer.Length - 1))
            Header = FullHeader.Split(" ")
            Message = Buffer.Substring(FullHeader.Length + 1)

            'Is this the NOTICE AUTH announce ?
            If FullHeader.Contains("NOTICE AUTH") Then
                'This is the network we are connected to
                _Server = Header(0)
            End If

            'Is this a server announce ?
            If (Header(0) = Server) Then
                'Parse the server announce
                ParseServerAnnounce(FullHeader, Header, Message)
                RaiseEvent OnRawServerAnnounce(FullHeader, Header, Message)
            Else
                'Parse user message
                ParseUserMessage(FullHeader, Header, Message)
                RaiseEvent OnRawUserMessage(FullHeader, Header, Message)
            End If
        Else
            'Extract the message header and body
            Pos = Buffer.IndexOf(":", 1)
            Header = Buffer.Substring(0, IIf(Pos > 0, Pos, Buffer.Length)).Split(" ")
            Message = Buffer.Substring(String.Join(" ", Header).Length + 1)

            'Parse the message
            Select Case Header(0)
                Case "PING"
                    'Pong
                    Send("PONG :" + Message)
                Case "ERROR"
                    'Error
                    Reason = Message
                    Socket.Close()
            End Select
        End If

    End Sub

    Private Sub ParseServerAnnounce(ByVal FullHeader As String, ByVal Header As String(), ByVal Message As String)
        'Parse the server announce
        Select Case Header(1)
            Case "001"
                'Ask for our user mask
                Send("WHO " + Nick)

                'Connected
                RaiseEvent OnConnect(Header(0))
            Case "352"
                If Header(7) = Nick Then
                    'Our user mask
                    _Mask = (Header(2) + "!" + Header(4) + "@" + Header(5))
                End If
        End Select

    End Sub

    Private Sub ParseUserMessage(ByVal FullHeader As String, ByVal Header As String(), ByVal Message As String)

        Dim Pos As Integer
        Dim Channel As Channel
        Dim User As User

        'Parse user message
        Select Case Header(1).ToUpper
            Case "PRIVMSG"
                'Is this a CTCP ?
                If Message.StartsWith(Chr(1)) And Message.EndsWith(Chr(1)) Then
                    'CTCP
                    Pos = Message.IndexOf(" ")
                    User = New User(Me, Header(0))
                    RaiseEvent OnCTCP(User, Message.Substring(1, IIf(Pos > 0, Pos - 1, Message.Length - 2)), IIf(Pos > 0, Message.Substring(Pos + 1, Message.Length - Pos - 2), String.Empty))
                Else
                    'Channel message
                    If Channels.ContainsKey(Header(2)) Then
                        Channel = Channels(Header(2))

                        If Channel.Users.ContainsKey(Header(0)) Then
                            User = Channel.Users(Header(0))
                        Else
                            'Unknown user
                            User = New User(Me, Header(0))
                            Channel.Users.Add(Header(0), User)
                        End If

                        RaiseEvent OnChannelMessage(Channel, User, Message)
                    End If
                End If

            Case "JOIN"
                If (Header(0) = Mask) Then
                    'We joined the channel :O
                    Channel = New Channel(Me, Message)
                    Channels.Add(Message, Channel)

                    'Synchronize the user list
                    Channel.Sync()

                    'Notify
                    RaiseEvent OnJoin(Channels(Message))
                Else
                    'HACK: a hack to workaround a very wierd bug on Safko's box
                    If Not Channels.ContainsKey(Message) Then
                        Channel = New Channel(Me, Message)
                        Channels.Add(Message, Channel)
                    End If

                    'User join
                    Channel = Channels(Message)

                    'Register this new user
                    User = New User(Me, Header(0))

                    If Not Channel.Users.ContainsKey(Header(0)) Then
                        'Add user to channel
                        Channel.Users.Add(Header(0), User)
                    End If

                    'Notify
                    RaiseEvent OnChannelJoin(Channel, User)
                End If

            Case "PART"
                If (Header(0) = Mask) Then
                    'We left the channel :(
                    RaiseEvent OnPart(Channels(Header(2)))
                    Channels.Remove(Header(2))
                Else
                    'User leave
                    Channel = Channels(Header(2))

                    'Notify
                    RaiseEvent OnChannelPart(Channel, Channel.Users(Header(0)), Message)

                    'Purge the user from our list
                    Channel.Users.Remove(Header(0))
                End If

            Case "KICK"
                If (Header(3) = Nick) Then
                    'Our ass is kicked >:|
                    Channel = Channels(Header(2))
                    RaiseEvent OnKick(Channel.Users(Header(0)), Channel, Message)
                    Channels.Remove(Header(2))
                Else
                    'Someone else is kicked :]
                    Channel = Channels(Header(2))
                    RaiseEvent OnChannelKick(Channel, Channel.Users(Header(0)), Header(3), Message)
                End If

            Case "NICK"
                If (Header(0) = Mask) Then
                    'Notify
                    RaiseEvent OnNickChange(Nick, Message)

                    'We changed our nick
                    _Mask = Mask.Remove(0, Nick.Length)
                    _Mask = Mask.Insert(0, Message)
                    _Nick = Message
                Else
                    'Someone else changed their nick
                    Dim Pair As KeyValuePair(Of String, Channel)

                    For Each Pair In Channels
                        'Resolve the channel
                        Channel = Pair.Value

                        'Where are you ~
                        If Channel.Users.ContainsKey(Mask) Then
                            'Found ya
                            User = Channel.Users(Mask)

                            'Update the nick
                            User.Nick = Message
                        End If
                    Next
                End If

        End Select

    End Sub

    Public ReadOnly Property Nick() As String
        Get
            Nick = _Nick
        End Get
    End Property

    Public ReadOnly Property Mask() As String
        Get
            Mask = _Mask
        End Get
    End Property

    Public ReadOnly Property Server() As String
        Get
            Server = _Server
        End Get
    End Property

    Public Sub Poll()
        'Poll for message from the IRC network
        IrcMessagePoll()
    End Sub

    Public Sub Connect(ByVal Server As String, ByVal Port As Integer, ByVal Nick As String, ByVal User As String)

        'Abort if already connected
        If (Not IrcThread Is Nothing) Then If (IrcThread.IsAlive) Then Exit Sub

        Try
            'Connect to the specified network
            Socket = New TcpClient(Server, Port)
            Stream = Socket.GetStream()

            'Initialize all the stream reader and writer
            Reader = New StreamReader(Stream)
            Writer = New StreamWriter(Stream)

            'Save the info
            _Nick = Nick

            'Initiate the IRC session
            Send("NICK " + Nick)                                             'Assign our nickname
            Send("USER " + Nick + " " + Nick + " " + Server + " :" + User)   'Register our user string

            'Start the bot thread
            IrcThread = New Thread(New ThreadStart(AddressOf IrcThreadStart))
            IrcThread.Start()

        Catch ex As Exception
            'Exception occurred
            RaiseEvent OnException(ex)
        End Try

    End Sub

    Public Sub Disconnect()

        If Not IrcThread Is Nothing Then
            If IrcThread.IsAlive Then
                'Disconnect from the network
                Socket.Close()

                'Wait for the thread to die
                While IrcThread.IsAlive
                    Application.DoEvents()
                End While
            End If
        End If

    End Sub

    Public Sub Send(ByVal Data As String)
        Writer.WriteLine(Data)
        Writer.Flush()

        'Notify
        RaiseEvent OnRawMessageSent(Data)
    End Sub

    Public Sub CTCP(ByVal Target As String, ByVal CTCP As String, Optional ByVal Params As String = "")
        'Send a CTCP to the specified target
        Message(Target, Chr(1) + CTCP + IIf(String.IsNullOrEmpty(Params), String.Empty, (" " + Params)) + Chr(1))
    End Sub

    Public Sub NCTCP(ByVal Target As String, ByVal CTCP As String, Optional ByVal Params As String = "")
        'Send a CTCP reply to the specified target
        Notify(Target, Chr(1) + CTCP + IIf(String.IsNullOrEmpty(Params), String.Empty, (" " + Params)) + Chr(1))
    End Sub

    Public Sub Message(ByVal Target As String, ByVal Message As String)
        'Say something
        Send("PRIVMSG " + Target + " :" + Message)
    End Sub

    Public Sub Notify(ByVal Target As String, ByVal Message As String)
        'Send a notice
        Send("NOTICE " + Target + " :" + Message)
    End Sub

    Public Sub SetNick(ByVal Nick As String)
        'Change to specified nick
        Send("NICK " + Nick)
    End Sub

    Public Sub Identify(ByVal Password As String)
        'Identify
        Message("NICKSERV", "IDENTIFY " + Password)
    End Sub

    Public Sub Ghost(ByVal Nick As String, ByVal Password As String)
        'Ghost the specified nick
        Message("NICKSERV", "GHOST " + Nick + " " + Password)
    End Sub

    Public Sub ChannelJoin(ByVal Channel As String)
        'Join the specified channel
        Send("JOIN " + Channel)
    End Sub

    Public Sub ChannelPart(ByVal Channel As String, Optional ByVal Reason As String = "")
        'Join the specified channel
        Send("PART " + Channel + IIf(Not String.IsNullOrEmpty(Reason), (" " + Reason), String.Empty))
    End Sub

End Class
