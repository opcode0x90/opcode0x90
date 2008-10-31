Imports MySql.Data.MySqlClient
Imports System.Math
Imports System.Data
Imports System.Text.RegularExpressions

Public Class IRCBot

    'IRCBot variables
    Public Network As String = "irc.p2p-network.net"
    Public Port As Integer = 6667
    Public Nick As String = "noppy"
    Public Nick2 As String = "_noppy"
    Public NickPassword As String = "seriouslysecurepassword"
    Public User As String = "I r stalker"
    Public Channel As String = "#cef"
    Public LogChannel As String = "#cef-loginfo"

    Public MaxXrefs As Integer = 10
    Public MaxResults As Integer = 3

    'MySQL database variables
    Public DataSource As String = "localhost"
    Public Database As String = "ircusers"
    Public UserID As String = "root"
    Public Password As String = "123"

    'IRC client
    Public WithEvents IRC As IRC = New IRC

    'MySQL Database Connection
    Private MySQL As MySqlConnection = New MySqlConnection("Data Source=" + DataSource + ";" + _
                                                           "User ID=" + UserID + ";" + _
                                                           "Password=" + Password + ";")

    'IRCBot exceptions
    Public Event OnException(ByVal ex As Exception)

    Private Function TruncateHost(ByVal Host As String) As String
        'Truncate the host name
        Host = Regex.Replace(Host, "P2PNET-[A-F0-9]{7,8}", "P2PNET-*")
        Host = Regex.Replace(Host, "mmoxeno-[A-F0-9]{7,8}", "mmoxeno-*")
        Host = Regex.Replace(Host, "[A-F0-9]{7,8}\.[A-F0-9]{7,8}\.[A-F0-9]{7,8}\.IP", "*.*.*.IP")

        'Done
        Return Host
    End Function

    '
    'This function will try to determine wheater the specified user
    'is connecting from a dynamic host
    '
    Private Function IsDynamicHost(ByVal User As User) As Boolean

        Dim Truncated As String

        'Is he using vhost ?
        Truncated = TruncateHost(User.Host)

        If (Truncated <> User.Host) Then
            'Check our database for any evidence of dynamic host
            If ExecuteScalar("SELECT Count(*) FROM users WHERE Nick = @1 AND Identd = @2 AND Host = @3 AND RealName = @4;", User.Nick, User.Identd, Truncated, User.RealName) = 0 Then
                'Well that is obvious, already previously identified as dynamic host
                Return True
            Else
                'Matches the P2PNET-XXXXXXXX dynamic host pattern

            End If
        End If

    End Function

    Private Function AccessCheck(ByVal User As User, ByVal Access As String) As Boolean

        Dim Result As MySqlDataReader
        Dim Granted As Boolean = False

        'Get all the ACL
        Result = ExecuteReader("SELECT Mask, Access FROM access")

        If Result.HasRows Then
            'Is access granted ?
            While Result.Read
                'Matches the user mask ?
                If User.Mask.ToLower Like Result("Mask").ToString.ToLower Then
                    'Do you have access ?
                    If (Result("Access") Like ("*" & Access & "*")) Or (Result("Access") = "*") Then
                        'Access granted
                        Granted = True
                        Exit While
                    End If
                End If
            End While
        Else
            'ACL is empty, grant all access
            Granted = True
        End If

        'Cleanup
        Result.Close()

        'Done
        Return Granted

    End Function

    Private Function ExecuteScalar(ByVal CommandText As String, ByVal ParamArray Parameters As Object()) As Object

        Dim Command As MySqlCommand = MySQL.CreateCommand()
        Dim i As Integer

        'Prepare the query
        Command.CommandText = CommandText

        'Fill in the parameters
        For i = 0 To Parameters.GetUpperBound(0)
            Command.Parameters.AddWithValue("@" & (i + 1), Parameters(i))
        Next

        'Return the result
        Return Command.ExecuteScalar

    End Function

    Private Sub ExecuteNonQuery(ByVal CommandText As String, ByVal ParamArray Parameters As Object())

        Dim Command As MySqlCommand = MySQL.CreateCommand
        Dim i As Integer

        'Prepare the query
        Command.CommandText = CommandText

        'Fill in the parameters
        For i = 0 To Parameters.GetUpperBound(0)
            Command.Parameters.AddWithValue("@" & (i + 1), Parameters(i))
        Next

        'Execute the command
        Command.ExecuteNonQuery()

    End Sub

    Private Function ExecuteReader(ByVal CommandText As String, ByVal ParamArray Parameters As Object()) As MySqlDataReader

        Dim Command As MySqlCommand = MySQL.CreateCommand
        Dim i As Integer

        'Prepare the query
        Command.CommandText = CommandText

        'Fill in the parameters
        For i = 0 To Parameters.GetUpperBound(0)
            Command.Parameters.AddWithValue("@" & (i + 1), Parameters(i))
        Next

        'Return the result
        Return Command.ExecuteReader

    End Function

    Private Function ExecuteDataSet(ByVal CommandText As String, ByVal ParamArray Parameters As Object()) As DataSet

        Dim Command As MySqlCommand = MySQL.CreateCommand

        Dim Adapter As MySqlDataAdapter
        Dim DataSet As DataSet = New DataSet

        'Prepare the query
        Command.CommandText = CommandText

        'Fill in the parameters
        For i = 0 To Parameters.GetUpperBound(0)
            Command.Parameters.AddWithValue("@" & (i + 1), Parameters(i))
        Next

        'Prepare the adapter
        Adapter = New MySqlDataAdapter(Command)

        'Populate the data set
        Adapter.Fill(DataSet)

        'Done
        Return DataSet

    End Function

    Public Function Start() As Boolean

        Try
            'Connect to localhost MySQL database
            MySQL.Open()

            'Create our database
            ExecuteNonQuery("CREATE DATABASE IF NOT EXISTS " + Database + ";")
            MySQL.ChangeDatabase(Database)

            'Generate the layout for our database
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS `users` (" + _
                                "`Index` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT," + _
                                "`Nick` VARCHAR(45) NOT NULL," + _
                                "`Identd` VARCHAR(45) NOT NULL," + _
                                "`Host` VARCHAR(200) NOT NULL," + _
                                "`RealName` VARCHAR(45) NOT NULL," + _
                                "PRIMARY KEY (`Index`)" + _
                            ");")

            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS `access` (" + _
                                "`Index` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT," + _
                                "`Nick` VARCHAR(45) NOT NULL," + _
                                "`Mask` VARCHAR(200) NOT NULL," + _
                                "`Access` CHAR(23) NOT NULL," + _
                                "PRIMARY KEY (`Index`)" + _
                            ");")

            'Connect to the IRC network
            IRC.Connect(Network, Port, Nick2, User)

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
        'Is this the right channel ?
        If Channel.Name = Me.Channel Then

            Dim SQL As MySqlCommand = MySQL.CreateCommand
            Dim Result As MySqlDataReader

            Dim Count As Integer = 0

            'Lookup for all the related entries
            If ExecuteScalar("SELECT Count(*) FROM users WHERE Nick = @1 AND Identd = @2 AND Host = @3 AND RealName = @4;", User.Nick, User.Identd, (User.Host), User.RealName) = 0 Then
                'Register this user in the database
                ExecuteNonQuery("INSERT INTO users (Nick, Identd, Host, RealName) VALUES (@1, @2, @3, @4)", User.Nick, User.Identd, (User.Host), User.RealName)
            End If

            'Lookup for all the related entries
            Result = ExecuteReader("SELECT * FROM users WHERE Nick = @1 OR Identd = @2 OR Host = @3 OR RealName = @4" & IIf(MaxResults > 0, " ORDER BY `Index` DESC LIMIT " & (MaxResults + 1) & ";", String.Empty), User.Nick, User.Identd, (User.Host), User.RealName)

            While Result.Read
                Dim Nick As String = Result("Nick")
                Dim Mask As String = (Result("Identd") & "@" & Result("Host"))
                Dim RealName As String = Result("RealName")

                'Log the user join
                Channel.Message("Nick: " & Nick & " | Mask: " & Mask & " | Real Name: " & RealName)
                Count += 1

                If (Count > MaxResults) And (MaxResults > 0) Then
                    'Snip
                    IRC.Message(LogChannel, "*** Excess results truncated ***")
                    Exit While
                End If
            End While

            'Formatting
            IRC.Message(LogChannel, " ")

            'Cleanup
            Result.Close()
        End If

    End Sub

    Private Sub IRC_OnChannelMessage(ByVal Channel As Channel, ByVal User As User, ByVal Message As String) Handles IRC.OnChannelMessage

        Dim Command As String()

        Dim Count As Integer = 0
        Dim RCount As Integer = 0
        Dim Result As MySqlDataReader

        Dim Entry As String = String.Empty

        'Listen only message from the log channel
        If (Channel.Name = LogChannel) Then
            'Is this a command ?
            If Message.StartsWith("!") Then
                'Handle command here
                Command = Message.Remove(0, 1).Split(" ")

                Select Case Command(0).ToLower
                    Case "access", "a"
                        'Access check
                        If Not AccessCheck(User, "a") Then
                            'Access denied
                            User.Notify("Access denied")
                            Exit Sub
                        End If

                        If Command.Length < 2 Then
                            'Are you sure you know what youre doing?
                            User.Notify("Available !access commands")
                            User.Notify(" ")
                            User.Notify("  add      Add user to access list")
                            User.Notify("  remove   Remove user from access list")
                            User.Notify("  update   Update the user mask/access")
                            User.Notify("  list     List the current access list")
                            User.Notify(" ")
                            Exit Sub
                        End If

                        'Access control list
                        Select Case Command(1).ToLower
                            Case "add", "a"
                                'Add someone to access list
                                If Command.Length < 5 Then
                                    'Invalid command
                                    User.Notify("Usage: !access add [Nick] [Mask] [Access]")
                                    User.Notify(" ")
                                    User.Notify("eg. !access add oppy *!*@I.nop.code adqv")
                                    User.Notify(" ")
                                    User.Notify("List of available accesses")
                                    User.Notify("  * = Full access")
                                    User.Notify("  a = !access commands")
                                    User.Notify("  d = !db commands")
                                    User.Notify("  q = !quote commands")
                                    User.Notify("  v = !var commands")
                                    User.Notify(" ")
                                    Exit Sub
                                End If

                                'Check for duplicate nick
                                If ExecuteScalar("SELECT Count(*) FROM access WHERE Nick = @1", Command(2)) > 0 Then
                                    'Nick already exist
                                    Channel.Message("*** Nick " & Command(2) & " already exist")
                                    Exit Sub
                                Else
                                    'Let this guy into our club
                                    ExecuteNonQuery("INSERT INTO access (`Nick`, `Mask`, `Access`) VALUES (@1, @2, @3)", Command(2), Command(3), Command(4))
                                End If

                                'Report
                                Channel.Message("*** Access " & Command(4) & " granted to " & Command(2) & " | Mask: " & Command(3))

                                Dim Overlapping As Boolean = False

                                'Retrieve the whole access list
                                Result = ExecuteReader("SELECT * FROM access")

                                'Check for any overlapping grants
                                While Result.Read
                                    'Overlapped ?
                                    If (Command(3) Like Result("Mask")) And (Command(3) <> Result("Mask")) Then
                                        'Warn
                                        Channel.Message("*** Warning: overlapping mask on " & Result("Nick") & ": " & Result("Mask"))
                                    End If
                                End While

                                'Cleanup
                                Result.Close()

                            Case "delete", "del", "d"
                                'Your license is revoked
                                If Command.Length < 3 Then
                                    'Learn !command before you try to pick on somebody
                                    User.Notify("Usage: !access delete [Nick]")
                                    Exit Sub
                                End If

                                'Does the nick exist ?
                                If ExecuteScalar("SELECT Count(*) FROM access WHERE Nick = @1", Command(2)) > 0 Then
                                    'Remove
                                    ExecuteNonQuery("DELETE FROM access WHERE Nick = @1", Command(2))
                                    Channel.Message("*** Nick " & Command(2) & " is removed from access list. Sucks to be you.")
                                Else
                                    'No such nick exist
                                    Channel.Message("*** Nick " & Command(2) & " doesnt exist")
                                End If

                            Case "update", "u"
                                'Update the nick info
                                If Command.Length < 5 Then
                                    'Update your own brain
                                    User.Notify("Usage: !access update (Mask|Access) [Nick] [Value]")
                                    Exit Sub
                                End If

                                'Does the nick exist ?
                                If ExecuteScalar("SELECT Count(*) FROM access WHERE Nick = @1", Command(3)) > 0 Then
                                    'What is your wish ?
                                    Select Case Command(2).ToLower
                                        Case "mask", "m" : Entry = "Mask"
                                        Case "access", "a" : Entry = "Access"
                                    End Select

                                    'Update
                                    ExecuteNonQuery("UPDATE access SET " & Entry & " = @1 WHERE Nick = @2", Command(4), Command(3))

                                    'Done
                                    User.Notify("*** Nick " & Entry.ToLower & " updated")
                                Else
                                    'No such nick exist
                                    Channel.Message("*** Nick " & Command(3) & " doesnt exist")
                                End If

                            Case "list", "l"
                                'Dump the ACL
                                Result = ExecuteReader("SELECT * FROM access")

                                'Dump out the results
                                User.Notify("*** Access list for " & Nick & " ***")
                                User.Notify("  No   Access  Nick           Mask")
                                While Result.Read
                                    Count += 1

                                    Dim Access As String = Result("Access")
                                    Dim Nick As String = Result("Nick")
                                    Dim Mask As String = Result("Mask")

                                    User.Notify("  " & Count & Space(Max(5 - Count.ToString.Length, 0)) & Access & Space(Max(8 - Access.Length, 0)) & Nick & Space(Max(15 - Nick.Length, 0)) & Mask)
                                End While
                                User.Notify("*** End of access list ***")

                                'Formatting
                                User.Notify(" ")

                                'Cleanup
                                Result.Close()

                        End Select

                    Case "xref", "x"
                        'Nick name cross reference
                        If Command.Length < 3 Then
                            'Find help first
                            User.Notify("Usage: !xref (Nick|Ident|Host|Name|Mask) [Value]")
                            User.Notify(" ")
                            User.Notify("eg. !xref name Safko")
                            User.Notify("    !xref host *.dyn.optonline.net")
                            User.Notify(" ")
                            Exit Sub
                        End If

                        Select Case Command(1).ToLower
                            Case "nick", "n" : Entry = "Nick"
                            Case "ident", "i" : Entry = "Identd"
                            Case "host", "h" : Entry = "Host"
                            Case "name", "u" : Entry = "RealName"
                            Case "mask", "m" : Entry = "CONCAT(Identd, '@', Host)"
                        End Select

                        If Not String.IsNullOrEmpty(Entry) Then
                            'Cross reference by nick
                            Result = ExecuteReader("SELECT * FROM users WHERE " & Entry & " LIKE @1" & IIf(MaxXrefs > 0, " LIMIT " & (MaxXrefs + 1), String.Empty), String.Join(" ", Command, 2, Command.Length - 2).Replace("*", "%"))

                            'Dump out the results
                            While Result.Read
                                Dim Nick As String = Result("Nick")
                                Dim Mask As String = (Result("Identd") & "@" & Result("Host"))
                                Dim RealName As String = Result("RealName")

                                Channel.Message("Nick: " & Nick & " | Mask: " & Mask & " | Real Name: " & RealName)
                                Count += 1

                                If (Count > MaxXrefs) And (MaxXrefs > 0) Then
                                    'Snip
                                    Channel.Message("*** Excess results truncated ***")
                                    Exit While
                                End If
                            End While

                            'Is there any result ?
                            If Count = 0 Then
                                'Nope
                                Channel.Message("*** No references found")
                            End If

                            'Formatting
                            Channel.Message(" ")

                            'Cleanup
                            Result.Close()
                        End If

                    Case "db"
                        'Access check
                        If Not AccessCheck(User, "d") Then
                            'Access denied
                            User.Notify("Access denied")
                            Exit Sub
                        End If

                        If Command.Length < 2 Then
                            'Are you sure you know what youre doing?
                            User.Notify("Available !db commands")
                            User.Notify(" ")
                            User.Notify("  count    Show current nickname count")
                            User.Notify("  remove   Remove the specified entry from database")
                            User.Notify(" ")
                            Exit Sub
                        End If

                        'Database related commands
                        Select Case Command(1).ToLower
                            Case "count", "c"
                                'Nickname entries count
                                Count = ExecuteScalar("SELECT Count(*) FROM users")
                                Channel.Message("Current nickname entries: " & Count)

                            Case "remove", "r"
                                'Remove the specified entry from the database
                                If Command.Length < 3 Then
                                    'l2remove
                                    User.Notify("Usage: !db remove (Nick|Ident|Host|Name|Mask) [Entry]")
                                End If

                                Select Case Command(2).ToLower
                                    Case "nick", "n" : Entry = "Nick"
                                    Case "ident", "i" : Entry = "Identd"
                                    Case "host", "h" : Entry = "Host"
                                    Case "name", "u" : Entry = "RealName"
                                    Case "mask", "m" : Entry = "CONCAT(Identd, '@', Host)"
                                End Select

                                If Not String.IsNullOrEmpty(Entry) Then
                                    'Report how many entries will be removed
                                    Count = ExecuteScalar("SELECT Count(*) FROM users WHERE " & Entry & " LIKE @1", String.Join(" ", Command, 3, Command.Length - 3).Replace("*", "%"))
                                    Channel.Message("*** Database entries removed: " & Count)

                                    'Now remove it for real
                                    ExecuteNonQuery("DELETE FROM users WHERE " & Entry & " = @1", String.Join(" ", Command, 3, Command.Length - 3))
                                End If

                        End Select

                    Case "quote", "q"
                        'Access check
                        If Not AccessCheck(User, "q") Then
                            'Access denied
                            User.Notify("Access denied")
                            Exit Sub
                        End If

                        If Command.Length < 2 Then
                            'Are you sure you know what youre doing?
                            User.Notify("Available !quote commands")
                            User.Notify(" ")
                            User.Notify("  raw     Send raw text to server")
                            User.Notify("  join    Make the bot join the channel")
                            User.Notify("  part    Make the bot leave the channel")
                            User.Notify("  say     Talk crap and brag")
                            User.Notify(" ")
                            Exit Sub
                        End If

                        'Say something
                        Select Case Command(1).ToLower
                            Case "raw", "r"
                                'Raw quote
                                IRC.Send(String.Join(" ", Command, 2, Command.Length - 2))

                            Case "join", "j"
                                If Command.Length < 3 Then
                                    'Are you sure you know what youre doing?
                                    User.Notify("Usage: !quote join [Channel]")
                                    Exit Sub
                                End If

                                'Join the specified channel
                                IRC.ChannelJoin(Command(2))

                            Case "part", "p"
                                If Command.Length < 3 Then
                                    'Are you sure you know what youre doing?
                                    User.Notify("Usage: !quote part [Channel] [Reason]")
                                    Exit Sub
                                End If

                                'Leave the specified channel
                                IRC.ChannelPart(Command(2), String.Join(" ", Command, 3, Command.Length - 3))

                            Case "say", "s"
                                If Command.Length < 4 Then
                                    'Are you sure you know what youre doing?
                                    User.Notify("Usage: !quote say [Channel] [Message]")
                                    Exit Sub
                                End If

                                'Say something
                                IRC.Message(Command(2), String.Join(" ", Command, 3, Command.Length - 3))

                        End Select

                    Case "var", "v"
                        'Access check
                        If Not AccessCheck(User, "v") Then
                            'Access denied
                            User.Notify("Access denied")
                            Exit Sub
                        End If

                        If Command.Length < 2 Then
                            'Are you sure you know what youre doing?
                            User.Notify("Available !var commands")
                            User.Notify(" ")
                            User.Notify("  logchannel     Sets current bot log channel. Dont change this unless you know what youre doing, you might lock yourself out from the bot")
                            User.Notify("  mask           Displays the bot current mask. For debugging purpose only")
                            User.Notify("  maxresults     Sets the maximum number of results to display when a user joins the channe;")
                            User.Notify("  maxxrefs       Sets the maximum number of results to display on !xrefs command")
                            User.Notify(" ")
                            Exit Sub
                        End If

                        'Bot variables
                        Select Case Command(1).ToLower
                            Case "logchannel"
                                'Log channel
                                If Command.Length > 2 Then
                                    'Set
                                    LogChannel = Command(2)

                                    'Join the specified channel
                                    IRC.ChannelJoin(Command(2))
                                End If

                                'Dump the variable
                                Channel.Message("*** LogChannel = " & LogChannel)

                            Case "mask"
                                'Dump the variable
                                Channel.Message("*** Mask = " & IRC.Mask)

                            Case "maxresults"
                                'Max results to display when a user join the channel
                                If Command.Length > 2 Then
                                    If IsNumeric(Command(2)) Then
                                        'Set
                                        MaxResults = Command(2)
                                    End If
                                End If

                                'Dump the variable
                                Channel.Message("*** MaxResults = " & MaxResults)

                            Case "maxxrefs"
                                'Max Xrefs results to display
                                If Command.Length > 2 Then
                                    If IsNumeric(Command(2)) Then
                                        'Set
                                        MaxXrefs = Command(2)
                                    End If
                                End If

                                'Dump the variable
                                Channel.Message("*** MaxXrefs = " & MaxXrefs)

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
