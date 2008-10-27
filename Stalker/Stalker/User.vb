
Public Class User

    'User fields
    Private _Mask As String
    Private _User As String
    Private _Mode As String

    Private _Nick As String         'Extracted from _Mask
    Private _Ident As String        'Extracted from _Mask
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
        Me.Sync()

    End Sub

    Public Sub New(ByVal IRC As IRC, ByVal Mask As String, ByVal User As String, ByVal Mode As String)
        'Initialize this user
        Me.IRC = IRC
        Me.Mask = Mask
        Me._User = User
        Me.Mode = Mode
        Me.IsSync = True
    End Sub

    Public ReadOnly Property Nick() As String
        Get
            Nick = Me._Nick
        End Get
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
            Me._Ident = Slice(1)
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

    Public ReadOnly Property User() As String
        Get
            User = Me._User
        End Get
    End Property

    Public ReadOnly Property Prefix() As String
        Get
            Prefix = Me._Prefix
        End Get
    End Property

    Public ReadOnly Property Ident() As String
        Get
            Ident = Me._Ident
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

    Private Sub IRC_OnRawServerAnnounce(ByVal FullHeader As String, ByVal Header() As String, ByVal Message As String) Handles IRC.OnRawServerAnnounce
        'Is this a /WHO list ?
        If Header(1) = "352" Then
            If Header(4) = Me.Ident And Header(5) = Me.Host And Header(7) = Me.Nick Then
                'Grab the user string and mode
                Me.Mode = Header(8)
                Me._User = Message.Substring(2)

                'Synchronized
                Me.IsSync = True
            End If
        End If
    End Sub

End Class
