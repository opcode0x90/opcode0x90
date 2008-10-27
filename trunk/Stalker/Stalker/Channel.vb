
Public Class Channel

    'Channel fields
    Private _Name As String
    Private _Users As Dictionary(Of String, User) = New Dictionary(Of String, User)

    'IRC network this channel is attached to
    Private WithEvents IRC As IRC

    Public Sub New(ByVal Name As String, ByVal IRC As IRC)
        'Attach to the specified network
        Me._Name = Name
        Me.IRC = IRC
    End Sub

    Public ReadOnly Property Name() As String
        Get
            Name = Me._Name
        End Get
    End Property

    Public ReadOnly Property Users() As Dictionary(Of String, User)
        Get
            Users = Me._Users
        End Get
    End Property

    Public Sub Part(Optional ByVal Reason As String = "")
        'Leave this channel
        IRC.ChannelPart(Me.Name, Reason)
    End Sub

    Public Sub Sync()
        '
    End Sub

End Class
