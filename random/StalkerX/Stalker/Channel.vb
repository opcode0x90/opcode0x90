
Public Class Channel

    'Channel fields
    Private _Name As String
    Private _Users As Dictionary(Of String, User) = New Dictionary(Of String, User)

    'IRC network this channel is attached to
    Private WithEvents IRC As IRC

    Public Sub New(ByVal IRC As IRC, ByVal Name As String)
        'Attach to the specified network
        _Name = Name
        Me.IRC = IRC
    End Sub

    Public ReadOnly Property Name() As String
        Get
            Name = _Name
        End Get
    End Property

    Public ReadOnly Property Users() As Dictionary(Of String, User)
        Get
            Users = _Users
        End Get
    End Property

    Public Sub Part(Optional ByVal Reason As String = "")
        'Leave this channel
        IRC.ChannelPart(Name, Reason)
    End Sub

    Public Sub Sync()
        'Purge the old userlist
        Users.Clear()

        'Synchronize the user list
        IRC.Send("WHO " + Name)
    End Sub

    Public Sub Message(ByVal Message As String)
        'Say something on the channel
        IRC.Message(Name, Message)
    End Sub

    Private Sub IRC_OnRawServerAnnounce(ByVal FullHeader As String, ByVal Header() As String, ByVal Message As String) Handles IRC.OnRawServerAnnounce
        'Is this a /WHO list ?
        If Header(1) = "352" And Header(3) = Name Then

            Dim Mask As String
            Dim User As String
            Dim Mode As String

            'Generate the user mask and retrieve user mode
            Mask = (Header(7) + "!" + Header(4) + "@" + Header(5))
            Mode = Header(8)
            User = Message.Substring(2)

            If Not Users.ContainsKey(Mask) Then
                'Add this user into the list
                Users.Add(Mask, New User(IRC, Mask, User, Mode))
            End If
        End If

    End Sub

End Class
