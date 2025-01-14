Imports System.Runtime.InteropServices

''' <summary>
''' This File contains the framework necessary to use COM Interop to obtain access and
''' control a remoted Windows Media Player control.
''' 
''' Originally written by Jonathan Dibble, Microsoft Corporation
''' Converted to VB, and heavily refactored by Darin Higgins 2012
''' </summary>
''' <remarks>
''' The main "entry point" object here is WMPRemote, which is essentially a static class
''' that exposes 2 shared
''' functions to provide access to the ActiveX Windows Media Player control for
''' an already loaded instance of Windows Media Player.
''' 
''' This file also defines a private internal Form, that hosts the WMP control ActiveX
''' control, which serves as a wrapper to the real player.
''' 
''' Since you can't use GetObject() with WMP as you can with Office apps, the WMP "remoting"
''' technique is the 
''' only way to gain access to an already loaded instance of WMP.
''' 
''' Why would you want to?
''' 
''' The main reason is to monitor what is playing and whether anything is playing at all.
''' For most purposes, you are actually better off building a WMP Plugin than can be run
''' directly from within WMP.
''' 
''' But the ability to remotely interact with a loaded instance of WMP is a requirement
''' for certain kinds of apps.
''' 
''' Inside this project we use the <see cref="IOleObject"/> interface used by the Windows
''' Media Player ActiveX control.  We also use the <see cref="IOleClientSite"/> and <see cref="IOleServiceProvider"/> to tell the
''' WMP Control about us.  Finally, <see cref="RemoteHostInfo"/> implements <see cref="IWMPRemoteMediaServices"/> which tells
''' Windows Media Player that we want to remote the control and provides an application name which appears in Windows
''' Media Player (under the View menu option.)
''' </remarks>
Public Class WMPRemote
    ''' <summary>
    ''' since there can only be one MediaPlayer loaded at a time, these are all
    ''' shared singleton objects
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared _HostingForm As frmWMPRemote
    Private Shared _InternalPlayer As WMPRemoteAx
    Private Shared _RemotePlayer As WMPLib.WindowsMediaPlayer


    Public Shared ReadOnly Property IsWindowMediaPlayerLoaded() As Boolean
        Get
            Try
                Dim r = (Process.GetProcessesByName("wmplayer").GetUpperBound(0) + 1) > 0
                '---- if we can't find the process, it can't be loaded
                Return r
            Catch
                Return False
            End Try
        End Get
    End Property


    ''' <summary>
    ''' This property actually retrieves a reference to the Remoted Media
    ''' player. This reference supports all the functionality including events
    ''' of the WindowsMediaPlayer object.
    ''' You can also cast this reference to IWMPPlayer4, for instance, to access
    ''' functions of later revs of the interface
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property ActivePlayer As WMPLib.WindowsMediaPlayer
        Get
            Try
                '---- when user requests the active player
                '     if we haven't already initialized things
                '     we need to do so now.
                If _InternalPlayer Is Nothing Then
                    If _HostingForm Is Nothing Then
                        '---- if WMP isn't loaded, there can't be a current player
                        If WMPRemote.IsWindowMediaPlayerLoaded Then
                            _HostingForm = New frmWMPRemote
                            '---- showing the dialog will gather the required shared
                            '     variables and set things up
                            _HostingForm.ShowDialog()
                        End If
                    End If
                End If
            Catch ex As Exception
                '---- nothing really to do, return nothing
            End Try
            Return _RemotePlayer
        End Get
    End Property


    ''' <summary>
    ''' This is a private form solely used to site the Remote WMP control
    ''' There is no need to access this form outside this class
    ''' </summary>
    ''' <remarks></remarks>
    Private Class frmWMPRemote
        Inherits System.Windows.Forms.Form


#Region " Compiler generated code"
        '---- NOTE 
        '     The VB designer generated the following code.
        '     Since this form is strictly INTERNAL and should never
        '     need any additional controls on it or work done on it
        '     in the designer, i've move that code here, and removed the
        '     *.designer.vb file.


        Public Sub New()
            InitializeComponent()
        End Sub


        'Form overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.SuspendLayout()
            ' 
            ' FrmMain
            ' 
            Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
            Me.ClientSize = New System.Drawing.Size(292, 267)
            Me.Name = "FrmMain"
            Me.Text = "Remoted Player"
            Me.ResumeLayout(False)
        End Sub
#End Region


        Private Sub frmWMPRemote_Load(sender As Object, e As System.EventArgs) Handles Me.Load
            '---- create a new instance of our wrapper OCX
            '     BUT we won't know if it's "real" or not till
            '     we get the Attached event fired
            '     Also, since this is all internal, this form should normally
            '     never be displayed to the user
            Me.Opacity = 0
            Me.ShowInTaskbar = False
            _InternalPlayer = New WMPRemoteAx
            _InternalPlayer.Dock = System.Windows.Forms.DockStyle.Fill
            Me.Controls.Add(_InternalPlayer)
            Me.Hide()
        End Sub
    End Class


    ''' <summary>
    ''' This is the actual Windows Media Control ActiveX Control
    ''' This will get instantiated and sited on a hidden form
    ''' </summary>
    <System.Windows.Forms.AxHost.ClsidAttribute("{6bf52a52-394a-11d3-b153-00c04f79faa6}")>
    <ComVisible(True)>
    <ClassInterface(ClassInterfaceType.AutoDispatch)>
    Public Class WMPRemoteAx
        Inherits System.Windows.Forms.AxHost
        Implements IOleServiceProvider
        Implements IOleClientSite


        ''' <summary>
        ''' Default Constructor.
        ''' </summary>
        Public Sub New()
            '---- Provide the right clsid for the AXHost object
            MyBase.New("6bf52a52-394a-11d3-b153-00c04f79faa6")
        End Sub


        ''' <summary>
        ''' WMP will call this function to hook up interfaces
        ''' In here, we call SetClientSite on the WMP Control, passing it
        ''' the dotNet container (this instance.)
        ''' It's only after this method is called that we know for sure
        ''' we're really hooked up to WindowsMediaPlayer
        ''' </summary>
        Protected Overrides Sub AttachInterfaces()
            Try
                '---- Get the IOleObject for Windows Media Player.
                Dim oleObject As IOleObject = TryCast(Me.GetOcx(), IOleObject)

                '---- Set the Client Site for the WMP control.
                Dim a = TryCast(Me, IOleClientSite)
                oleObject.SetClientSite(a)

                '---- and finally, we can expose the MediaPlayer by
                '     setting this shared var in the main WMPRemote class
                _RemotePlayer = DirectCast(Me.GetOcx(), WMPLib.WindowsMediaPlayer)

            Catch ex As System.Exception
                '---- If this exception fires, there's really nothing
                '     to be done, other than make sure _RemotePlayer is null
                '     because we've failed to connect to the running WMP
                _RemotePlayer = Nothing
            End Try
        End Sub


#Region "IOleServiceProvider Members - Working"
        ''' <summary>
        ''' During SetClientSite, WMP calls this function to get the pointer to <see cref="RemoteHostInfo"/>.
        ''' </summary>
        ''' <param name="guidService">See MSDN for more information - we do not use this parameter.</param>
        ''' <param name="riid">The Guid of the desired service to be returned.  For this application it will always match
        ''' the Guid of <see cref="IWMPRemoteMediaServices"/>.</param>
        ''' <returns></returns>
        Private Function IOleServiceProvider_QueryService(ByRef guidService As Guid, ByRef riid As Guid) As IntPtr Implements IOleServiceProvider.QueryService
            'If we get to here, it means Media Player is requesting our IWMPRemoteMediaServices interface
            If riid = New Guid("cbb92747-741f-44fe-ab5b-f1a48f3b2a59") Then
                Dim iwmp As IWMPRemoteMediaServices = New RemoteHostInfo()
                Return Marshal.GetComInterfaceForObject(iwmp, GetType(IWMPRemoteMediaServices))
            End If

            Throw New System.Runtime.InteropServices.COMException("No Interface", CInt(HResults.E_NOINTERFACE))
        End Function
#End Region

#Region "IOleClientSite Members"
        ''' <summary>
        ''' Not in use.  See MSDN for details.
        ''' </summary>
        ''' <exception cref="System.Runtime.InteropServices.COMException">E_NOTIMPL</exception>
        Private Sub IOleClientSite_SaveObject() Implements IOleClientSite.SaveObject
            Throw New System.Runtime.InteropServices.COMException("Not Implemented", CInt(HResults.E_NOTIMPL))
        End Sub

        ''' <summary>
        ''' Not in use.  See MSDN for details.
        ''' </summary>
        ''' <exception cref="System.Runtime.InteropServices.COMException"></exception>
        Private Function IOleClientSite_GetMoniker(dwAssign As UInteger, dwWhichMoniker As UInteger) As Object Implements IOleClientSite.GetMoniker
            Throw New System.Runtime.InteropServices.COMException("Not Implemented", CInt(HResults.E_NOTIMPL))
        End Function

        ''' <summary>
        ''' Not in use.  See MSDN for details.
        ''' However, I've discovered that you can't just throw an exception here 
        ''' like the original demo.
        ''' Instead, I just re-declared this method and return an E_NOTIMPL
        ''' WMP Seems to be much happier with that arrangement and won't crash.
        ''' </summary>
        Public Function GetContainer(ByRef out As Object) As HResults Implements IOleClientSite.GetContainer
            Return HResults.E_NOTIMPL
        End Function


        ''' <summary>
        ''' Not in use.  See MSDN for details.
        ''' </summary>
        ''' <exception cref="System.Runtime.InteropServices.COMException"></exception>
        Private Sub IOleClientSite_ShowObject() Implements IOleClientSite.ShowObject
            Throw New System.Runtime.InteropServices.COMException("Not Implemented", CInt(HResults.E_NOTIMPL))
        End Sub

        ''' <summary>
        ''' Not in use.  See MSDN for details.
        ''' </summary>
        ''' <exception cref="System.Runtime.InteropServices.COMException"></exception>
        Private Sub IOleClientSite_OnShowWindow(fShow As Boolean) Implements IOleClientSite.OnShowWindow
            Throw New System.Runtime.InteropServices.COMException("Not Implemented", CInt(HResults.E_NOTIMPL))
        End Sub

        ''' <summary>
        ''' Not in use.  See MSDN for details.
        ''' </summary>
        ''' <exception cref="System.Runtime.InteropServices.COMException"></exception>
        Private Sub IOleClientSite_RequestNewObjectLayout() Implements IOleClientSite.RequestNewObjectLayout
            Throw New System.Runtime.InteropServices.COMException("Not Implemented", CInt(HResults.E_NOTIMPL))
        End Sub
#End Region
    End Class
End Class




''' <summary>
''' This class contains the information to return to Media Player about our remote service.
''' This has to be public and marked for COM because WMP expects to be able to 
''' call methods on it via COM
''' </summary>
<ComVisible(True)>
<ClassInterface(ClassInterfaceType.None)>
Public Class RemoteHostInfo
    Implements IWMPRemoteMediaServices

    ''' <summary>
    ''' Returns "Remote" to tell media player that we want to remote the WMP application.
    ''' </summary>
    ''' <returns></returns>
    Public Function GetServiceType() As String Implements IWMPRemoteMediaServices.GetServiceType
        Return "Remote"
    End Function


    ''' <summary>
    ''' The Application Name to show in Windows Media Player.
    ''' </summary>
    ''' <returns></returns>
    Public Function GetApplicationName() As String Implements IWMPRemoteMediaServices.GetApplicationName
        Return System.Diagnostics.Process.GetCurrentProcess().ProcessName
    End Function


    ''' <summary>
    ''' Not in use, see MSDN for more info.
    ''' </summary>
    ''' <param name="name"></param>
    ''' <param name="dispatch"></param>
    ''' <returns></returns>
    Public Function GetScriptableObject(ByRef name As String, ByRef dispatch As Object) As HResults Implements IWMPRemoteMediaServices.GetScriptableObject
        name = Nothing
        dispatch = Nothing
        Return HResults.E_NOTIMPL
    End Function


    ''' <summary>
    ''' For skins, not in use, see MSDN for more info.
    ''' </summary>
    ''' <param name="file"></param>
    ''' <returns></returns>
    Public Function GetCustomUIMode(ByRef file As String) As HResults Implements IWMPRemoteMediaServices.GetCustomUIMode
        file = Nothing
        Return HResults.E_NOTIMPL
    End Function
End Class




#Region "Useful COM Enums"
''' <summary>
''' Represents a collection of frequently used HRESULT values.
''' You may add more HRESULT VALUES, I've only included the ones used 
''' in this project.
''' </summary>
Public Enum HResults
    ''' <summary>
    ''' HRESULT S_OK
    ''' </summary>
    S_OK = &H0
    ''' <summary>
    ''' HRESULT S_FALSE
    ''' </summary>
    S_FALSE = &H1
    ''' <summary>
    ''' HRESULT E_NOINTERFACE
    ''' </summary>
    E_NOINTERFACE = &H80004002
    ''' <summary>
    ''' HRESULT E_NOTIMPL
    ''' </summary>
    E_NOTIMPL = &H80004001
End Enum


''' <summary>
''' Enumeration for <see cref="IOleObject.GetMiscStatus"/>
''' </summary>
Public Enum DVASPECT
    ''' <summary>
    ''' See MSDN for more information.
    ''' </summary>
    Content = 1
    ''' <summary>
    ''' See MSDN for more information.
    ''' </summary>
    Thumbnail = 2
    ''' <summary>
    ''' See MSDN for more information.
    ''' </summary>
    Icon = 3
    ''' <summary>
    ''' See MSDN for more information.
    ''' </summary>
    DocPrint = 4
End Enum
#End Region


#Region "IWMPRemoteMediaServices"
''' <summary>
''' Interface used by Media Player to determine WMP Remoting status.
''' </summary>
<ComImport(), ComVisible(True), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CBB92747-741F-44fe-AB5B-F1A48F3B2A59")>
Public Interface IWMPRemoteMediaServices

    ''' <summary>
    ''' Service type.
    ''' </summary>
    ''' <returns><code>Remote</code> if the control is to be remoted (attached to WMP.) 
    ''' <code>Local</code>if this is an independent WMP instance not connected to WMP application.  If you want local, you shouldn't bother
    ''' using this control!
    ''' </returns>
    Function GetServiceType() As <MarshalAs(UnmanagedType.BStr)> String

    ''' <summary>
    ''' Value to display in Windows Media Player Switch To Application menu option (under View.)
    ''' </summary>
    ''' <returns></returns>
    Function GetApplicationName() As <MarshalAs(UnmanagedType.BStr)> String

    ''' <summary>
    ''' Not in use, see MSDN for details.
    ''' </summary>
    ''' <param name="name"></param>
    ''' <param name="dispatch"></param>
    ''' <returns></returns>
    <PreserveSig()>
    Function GetScriptableObject(<MarshalAs(UnmanagedType.BStr)> ByRef name As String, <MarshalAs(UnmanagedType.IDispatch)> ByRef dispatch As Object) As <MarshalAs(UnmanagedType.U4)> HResults

    ''' <summary>
    ''' Not in use, see MSDN for details.
    ''' </summary>
    ''' <param name="file"></param>
    ''' <returns></returns>
    <PreserveSig()>
    Function GetCustomUIMode(<MarshalAs(UnmanagedType.BStr)> ByRef file As String) As <MarshalAs(UnmanagedType.U4)> HResults
End Interface
#End Region


#Region "IOleServiceProvider"
''' <summary>
''' Interface used by Windows Media Player to return an instance of IWMPRemoteMediaServices.
''' </summary>
<ComImport(), GuidAttribute("6d5140c1-7436-11ce-8034-00aa006009fa"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), ComVisible(True)>
Public Interface IOleServiceProvider
    ''' <summary>
    ''' Similar to QueryInterface, riid will contain the Guid of an object to return.
    ''' In our project we will look for <see cref="IWMPRemoteMediaServices"/> Guid and return the object
    ''' that implements that interface.
    ''' </summary>
    ''' <param name="guidService"></param>
    ''' <param name="riid">The Guid of the desired Service to provide.</param>
    ''' <returns>A pointer to the interface requested by the Guid.</returns>
    Function QueryService(ByRef guidService As Guid, ByRef riid As Guid) As IntPtr
End Interface


''' <summary>
''' This is an example of an INCORRECT entry - do not use, unless you want your app to break.
''' </summary>
<ComImport(), GuidAttribute("6d5140c1-7436-11ce-8034-00aa006009fa"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), ComVisible(True)>
Public Interface BadIOleServiceProvider
    ''' <summary>
    ''' This is incorrect because it causes our return interface to be boxed
    ''' as an object and a COM callee may not get the correct pointer.
    ''' </summary>
    ''' <param name="guidService"></param>
    ''' <param name="riid"></param>
    ''' <returns></returns>
    ''' <example>
    ''' For an example of a correct definition, look at <see cref="IOleServiceProvider"/>.
    ''' </example>
    Function QueryService(ByRef guidService As Guid, ByRef riid As Guid) As <MarshalAs(UnmanagedType.[Interface])> Object
End Interface
#End Region


#Region "IOleClientSite"
''' <summary>
''' Need to implement this interface so we can pass it to <see cref="IOleObject.SetClientSite"/>.
''' All functions return E_NOTIMPL.  We don't need to actually implement anything to get
''' the remoting to work.
''' </summary>
<ComImport(), ComVisible(True), Guid("00000118-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Public Interface IOleClientSite
    ''' <summary>
    ''' See MSDN for more information.  Throws <see cref="COMException"/> with id of E_NOTIMPL.
    ''' </summary>
    ''' <exception cref="COMException">E_NOTIMPL</exception>
    Sub SaveObject()

    ''' <summary>
    ''' See MSDN for more information.  Throws <see cref="COMException"/> with id of E_NOTIMPL.
    ''' </summary>
    ''' <exception cref="COMException">E_NOTIMPL</exception>
    Function GetMoniker(dwAssign As UInteger, dwWhichMoniker As UInteger) As <MarshalAs(UnmanagedType.[Interface])> Object

    ''' <summary>
    ''' //See MSDN for more information.  Throws <see cref="COMException"/> with id of E_NOTIMPL.
    ''' Scratch that. can't throw the exc in VB, doesn't work
    ''' instead, I've changed the declaration of this method and now I just return
    ''' notimpl
    ''' </summary>
    <PreserveSig()>
    Function GetContainer(<MarshalAs(UnmanagedType.[Interface])> ByRef out As Object) As <MarshalAs(UnmanagedType.U4)> HResults

    ''' <summary>
    ''' See MSDN for more information.  Throws <see cref="COMException"/> with id of E_NOTIMPL.
    ''' </summary>
    ''' <exception cref="COMException">E_NOTIMPL</exception>
    Sub ShowObject()

    ''' <summary>
    ''' See MSDN for more information.  Throws <see cref="COMException"/> with id of E_NOTIMPL.
    ''' </summary>
    ''' <exception cref="COMException">E_NOTIMPL</exception>
    Sub OnShowWindow(fShow As Boolean)

    ''' <summary>
    ''' See MSDN for more information.  Throws <see cref="COMException"/> with id of E_NOTIMPL.
    ''' </summary>
    ''' <exception cref="COMException">E_NOTIMPL</exception>
    Sub RequestNewObjectLayout()
End Interface
#End Region


#Region "IOleObject"
''' <summary>
''' This interface is implemented by WMP ActiveX/COM control.
''' The only function we need is SetClientSite.
''' </summary>
<ComImport(), ComVisible(True), Guid("00000112-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Public Interface IOleObject
    ''' <summary>
    ''' Used to pass our custom <see cref="IOleClientSite"/> object to WMP.  The object we pass must also
    ''' implement <see cref="IOleServiceProvider"/> to work right.
    ''' </summary>
    ''' <param name="pClientSite">The <see cref="IOleClientSite"/> to pass.</param>
    Sub SetClientSite(pClientSite As IOleClientSite)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function GetClientSite() As <MarshalAs(UnmanagedType.[Interface])> IOleClientSite

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub SetHostNames(<MarshalAs(UnmanagedType.LPWStr)> szContainerApp As String, <MarshalAs(UnmanagedType.LPWStr)> szContainerObj As String)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub Close(dwSaveOption As UInteger)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub SetMoniker(dwWhichMoniker As UInteger, pmk As Object)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function GetMoniker(dwAssign As UInteger, dwWhichMoniker As UInteger) As <MarshalAs(UnmanagedType.[Interface])> Object

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub InitFromData(pDataObject As Object, fCreation As Boolean, dwReserved As UInteger)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function GetClipboardData(dwReserved As UInteger) As Object


    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub DoVerb(iVerb As UInteger, lpmsg As UInteger, <MarshalAs(UnmanagedType.[Interface])> pActiveSite As Object, lindex As UInteger, hwndParent As UInteger, lprcPosRect As UInteger)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function EnumVerbs() As <MarshalAs(UnmanagedType.[Interface])> Object

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub Update()

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    <PreserveSig()>
    Function IsUpToDate() As <MarshalAs(UnmanagedType.U4)> HResults

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function GetUserClassID() As Guid

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function GetUserType(dwFormOfType As UInteger) As <MarshalAs(UnmanagedType.LPWStr)> String

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub SetExtent(dwDrawAspect As UInteger, <MarshalAs(UnmanagedType.[Interface])> psizel As Object)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function GetExtent(dwDrawAspect As UInteger) As <MarshalAs(UnmanagedType.[Interface])> Object

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function Advise(<MarshalAs(UnmanagedType.[Interface])> pAdvSink As Object) As UInteger

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub Unadvise(dwConnection As UInteger)

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function EnumAdvise() As <MarshalAs(UnmanagedType.[Interface])> Object

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Function GetMiscStatus(<MarshalAs(UnmanagedType.U4)> dwAspect As DVASPECT) As UInteger

    ''' <summary>
    ''' Implemented by Windows Media Player ActiveX control.
    ''' See MSDN for more information.
    ''' </summary>
    Sub SetColorScheme(<MarshalAs(UnmanagedType.[Interface])> pLogpal As Object)
End Interface
#End Region

