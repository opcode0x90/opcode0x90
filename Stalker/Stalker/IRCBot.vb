Imports MySql.Data.MySqlClient
Imports System.Text.RegularExpressions

Public Class IRCBot

    'IRCBot variables
    Public Network As String = "irc.p2p-network.net"
    Public Port As Integer = 6667
    Public Nick As String = "noppy"
    Public NickPassword As String = "seriouslysecurepassword"
    Public User As String = "I r stalker"
    Public Channel As String = "#cef"
    Public LogChannel As String = "#cef-loginfo"

    'MySQL database variables
    Public DataSource As String = "localhost"
    Public Database As String = "ircusers"
    Public UserID As String = "root"
    Public Password As String = "123"

    'IRC client
    Public WithEvents IRC As IRC = New IRC

    'MySQL Database Connection
    Private MySQL As MySqlConnection = New MySqlConnection("Data Source=" + DataSource + _
                                                           ";User ID=" + UserID + _
                                                           ";Password=" + Password + _
                                                           ";")

    'IRCBot exceptions
    Public Event OnException(ByVal ex As Exception)

    Private Function TruncateHost(ByVal Host As String) As String
        'Truncate the host name
        Host = Regex.Replace(Host, "P2PNET-[A-F0-9]{7,8}", "P2PNET-XXXXXXXX")
        Host = Regex.Replace(Host, "[A-F0-9]{7,8}\.[A-F0-9]{7,8}\.[A-F0-9]{7,8}\.IP", "XXXXXXXX.XXXXXXXX.XXXXXXXX.IP")

        'Done
        Return Host
    End Function

    Public Function Start() As Boolean

        Try
            Dim SQL As MySqlCommand

            'Connect to localhost MySQL database
            MySQL.Open()
            SQL = MySQL.CreateCommand()

            'Create our database
            SQL.CommandText = "CREATE DATABASE IF NOT EXISTS " + Database + ";"
            SQL.ExecuteNonQuery()
            MySQL.ChangeDatabase(Database)

            'Generate the layout for our database
            SQL.CommandText = "CREATE TABLE IF NOT EXISTS `users` (" + _
                                   "`Index` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT," + _
                                   "`Nick` VARCHAR(45) NOT NULL," + _
                                   "`Identd` VARCHAR(45) NOT NULL," + _
                                   "`Host` VARCHAR(45) NOT NULL," + _
                                   "`RealName` VARCHAR(45) NOT NULL," + _
                                   "PRIMARY KEY (`Index`)" + _
                               ");"
            SQL.ExecuteNonQuery()

            'Connect to the IRC network
            IRC.Connect(Network, Port, (Nick + Hex(Int(Rnd() * 1000))), User)

            'Done
            Return True

        Catch ex As Exception
            'Exception occurred
            RaiseEvent OnException(ex)
        End Try

    End Function

    Public Sub Kill()
        'Disconnect from the IRC network and database
        IRC.Disconnect()
        MySQL.Close()
    End Sub

    Private Sub IRC_OnChannelJoin(ByVal Channel As Channel, ByVal User As User) Handles IRC.OnChannelJoin

        Dim SQL As MySqlCommand = MySQL.CreateCommand
        Dim Result As MySqlDataReader

        Dim Count As Integer = 0

        'Lookup for all the related entries
        SQL.CommandText = "SELECT Count(*) FROM users WHERE Nick = @Nick AND Identd = @Identd AND Host = @Host AND RealName = @RealName;"
        SQL.Parameters.AddWithValue("@Nick", User.Nick)
        SQL.Parameters.AddWithValue("@Identd", User.Identd)
        SQL.Parameters.AddWithValue("@Host", TruncateHost(User.Host))
        SQL.Parameters.AddWithValue("@RealName", User.RealName)

        'Is there any result ?
        If SQL.ExecuteScalar = 0 Then
            'Register this user in the database
            SQL = MySQL.CreateCommand
            SQL.CommandText = "INSERT INTO users (Nick, Identd, Host, RealName) VALUES (@Nick, @Identd, @Host, @RealName)"
            SQL.Parameters.AddWithValue("@Nick", User.Nick)
            SQL.Parameters.AddWithValue("@Identd", User.Identd)
            SQL.Parameters.AddWithValue("@Host", TruncateHost(User.Host))
            SQL.Parameters.AddWithValue("@RealName", User.RealName)
            SQL.ExecuteNonQuery()
        End If

        'Lookup for all the related entries
        SQL = MySQL.CreateCommand
        SQL.CommandText = "SELECT * FROM users WHERE Nick = @Nick OR Identd = @Identd OR Host = @Host OR RealName = @RealName;"
        SQL.Parameters.AddWithValue("@Nick", User.Nick)
        SQL.Parameters.AddWithValue("@Identd", User.Identd)
        SQL.Parameters.AddWithValue("@Host", TruncateHost(User.Host))
        SQL.Parameters.AddWithValue("@RealName", User.RealName)
        Result = SQL.ExecuteReader

        While Result.Read
            'Log the user join
            IRC.Message(LogChannel, "Nick: " + Result("Nick") + " | Mask: " + (Result("Identd") + "@" + Result("Host")) + " | Real Name: " + Result("RealName"))
            Count += 1
        End While

        'Formatting
        IRC.Message(LogChannel, " ")

        'Cleanup
        Result.Close()

    End Sub

    Private Sub IRC_OnChannelMessage(ByVal Channel As Channel, ByVal User As User, ByVal Message As String) Handles IRC.OnChannelMessage

        Dim Count As Integer = 0
        Dim RCount As Integer = 0

        Dim Command As String()

        Dim SQL As MySqlCommand = MySQL.CreateCommand
        Dim Result As MySqlDataReader

        'Listen only message from the log channel
        If (Channel.Name = LogChannel) Then
            'Is this a command ?
            If Message.StartsWith("!") Then
                'Handle command here
                Command = Message.Remove(0, 1).Split(" ")

                Select Case Command(0).ToLower
                    Case "xref", "x"

                        Dim Entry As String = String.Empty

                        'Nick name cross reference
                        Select Case Command(1).ToLower
                            Case "nick", "n" : Entry = "Nick"
                            Case "ident", "i" : Entry = "Identd"
                            Case "host", "h" : Entry = "Host"
                            Case "name", "u" : Entry = "RealName"
                        End Select

                        If Not String.IsNullOrEmpty(Entry) Then
                            'Cross reference by nick
                            SQL.CommandText = "SELECT * FROM users WHERE " + Entry + " = @Value"
                            SQL.Parameters.AddWithValue("@Value", Command(2))
                            Result = SQL.ExecuteReader

                            'Dump out the results
                            While Result.Read
                                Channel.Message("Nick: " + Result("Nick") + " | Mask: " + (Result("Identd") + "@" + Result("Host")) + " | Real Name: " + Result("RealName"))
                                Count += 1
                            End While

                            'Is there any result ?
                            If Count = 0 Then
                                'Nope
                                Channel.Message("No references found")
                            End If

                            'Formatting
                            Channel.Message(" ")

                            'Cleanup
                            Result.Close()
                        End If

                    Case "db"
                        'Database related commands
                        Select Case Command(1).ToLower
                            Case "count"
                                'Nickname entries count
                                SQL.CommandText = "SELECT Count(*) FROM users"
                                Count = SQL.ExecuteScalar
                                Channel.Message("Current nickname entries: " + Count.ToString)

                            Case "cleanup"
                                'Cleanup the database


                        End Select

                    Case "quote", "q"
                        'Say something
                        Select Case Command(1).ToLower
                            Case "raw"
                                'Raw quote
                                IRC.Send(String.Join(" ", Command, 2, Command.Length - 2))
                        End Select

                    Case "var", "v"
                        'Bot variables
                        Select Case Command(1).ToLower
                            Case "logchannel"
                                'Log channel
                                '                                if Command.Length > 2 Then
                                '                                'Join the new channel
                                '                                IRC.ChannelPart(LogChannel)
                                '                                IRC.ChannelJoin(Command(2))
                                '
                                '                                'Set
                                '                                LogChannel = Command(2)
                                '                                End If

                                'Dump the variable
                                Channel.Message("LogChannel = " + LogChannel)

                            Case "mask"
                                'Dump the variable
                                Channel.Message("Mask = " + IRC.Mask)

                        End Select

                End Select
            End If
        End If

    End Sub

    Private Sub IRC_OnConnect(ByVal Server As String) Handles IRC.OnConnect
        'Identify
        IRC.Ghost(Nick, NickPassword)
        IRC.SetNick(Nick)
        IRC.Identify(NickPassword)

        'It takes a while to recognize me
        Threading.Thread.Sleep(3000)

        'Autojoin channel
        IRC.ChannelJoin(Channel)
        IRC.ChannelJoin(LogChannel)
    End Sub

    Private Sub IRC_OnCTCP(ByVal User As User, ByVal CTCP As String, ByVal Params As String) Handles IRC.OnCTCP
        'Filter out /me
        If CTCP.ToUpper <> "ACTION" Then
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
        End If
    End Sub

    Private Sub IRC_OnDisconnect(ByVal Reason As String) Handles IRC.OnDisconnect
        'Cleanup
        MySQL.Close()
    End Sub

    Private Sub IRC_OnKick(ByVal User As User, ByVal Channel As Channel, ByVal Reason As String) Handles IRC.OnKick
        'Who kick mah ass ? >:|
        IRC.ChannelJoin(Channel.Name)
        IRC.Message(Channel.Name, "Who kick mah ass >:|")
    End Sub

End Class
