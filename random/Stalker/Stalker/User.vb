
Public Class User

    'User fields
    Private _Mask As String
    Private _Mode As String
    Private _RealName As String

    Private _Nick As String         'Extracted from _Mask
    Private _Identd As String       'Extracted from _Mask
    Private _Host As String         'Extracted from _Mask
    Private _Prefix As String       'Extracted from _Mode

    Private IsSync As Boolean = False

    'IRC network this user is attached to
    Private WithEvents IRC As IRC

    Public Sub New(ByVal IRC As IRC, ByVal Mask As String)
        'Initialize this user with only mask
        Me.IRC = IRC
        Me.Mask = Mask

        'Synchronize the user info
        SyncNonAsync()

    End Sub

    Public Sub New(ByVal IRC As IRC, ByVal Mask As String, ByVal RealName As String, ByVal Mode As String)
        'Initialize this user
        Me.IRC = IRC
        Me.Mask = Mask
        Me._RealName = RealName
        Me.Mode = Mode
        Me.IsSync = True
    End Sub

    Public Property Nick() As String
        Get
            Nick = Me._Nick
        End Get
        Set(ByVal value As String)
            'We changed our nick
            _Mask = Mask.Remove(0, _Nick.Length)
            _Mask = Mask.Insert(0, value)
            _Nick = value
        End Set
    End Property

    Public Property Mask() As String
        Get
            Mask = Me._Mask
        End Get
        Private Set(ByVal value As String)
            Me._Mask = value

            Dim Separator As Char() = "!@"
            Dim Slice As String() = Me.Mask.Split(Separator, 3)

            Me._Nick = Slice(0)
            Me._Identd = Slice(1)
            Me._Host = Slice(2)

        End Set
    End Property

    Public Property Mode() As String
        Get
            Mode = Me._Mode
        End Get
        Private Set(ByVal value As String)
            Me._Mode = value

            Dim prefix As Char = Me.Mode.Last

            'Is this a prefix ?
            If Not Char.IsLetterOrDigit(prefix) Then
                'Yup
                Me._Prefix = prefix
            End If

        End Set
    End Property

    Public ReadOnly Property RealName() As String
        Get
            RealName = Me._RealName
        End Get
    End Property

    Public ReadOnly Property Prefix() As String
        Get
            Prefix = Me._Prefix
        End Get
    End Property

    Public ReadOnly Property Identd() As String
        Get
            Identd = Me._Identd
        End Get
    End Property

    Public ReadOnly Property Host() As String
        Get
            Host = Me._Host
        End Get
    End Property

    Public Sub Sync()
        'Synchronize the user info
        IRC.Send("WHO " + Me.Nick)
    End Sub

    Public Sub SyncNonAsync()
        'Synchronize the user info
        Me.IsSync = False
        Me.Sync()

        'Wait until the user info is synchronized
        While Not Me.IsSync
            'Keep polling for message
            IRC.Poll()
        End While

    End Sub

    Public Sub Notify(ByVal Message As String)
        'Notify
        IRC.Notify(Nick, Message)
    End Sub

    Private Sub IRC_OnRawServerAnnounce(ByVal FullHeader As String, ByVal Header() As String, ByVal Message As String) Handles IRC.OnRawServerAnnounce
        'Is this a /WHO list ?
        If Header(1) = "352" Then
            If Header(4) = Me.Identd And Header(5) = Me.Host And Header(7) = Me.Nick Then
                'Grab the user string and mode
                Me.Mode = Header(8)
                Me._RealName = Message.Substring(2)

                'Synchronized
                Me.IsSync = True
            End If
        End If
    End Sub

End Class
